# TCC — Comparação de Mecanismos de Comunicação entre Microserviços

Experimento controlado comparando **REST**, **gRPC** e **Kafka** como mecanismos de comunicação entre microserviços.

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Docker Compose](https://docs.docker.com/compose/)
- [k6](https://k6.io/docs/get-started/installation/)

---

## Estrutura do Projeto

```
tcc-microservices/
├── src/
│   ├── OrderGateway/
│   │   ├── Http/        # Gateway REST
│   │   ├── Grpc/        # Gateway gRPC
│   │   └── Kafka/       # Gateway Kafka
│   └── OrderProcessor/
│       ├── Http/        # Processor REST
│       ├── Grpc/        # Processor gRPC
│       └── Kafka/       # Processor Kafka
├── proto/
│   └── order.proto      # Contrato gRPC compartilhado
├── compose/
│   ├── docker-compose.rest.yml
│   ├── docker-compose.grpc.yml
│   └── docker-compose.kafka.yml
├── observability/
│   ├── prometheus/
│   │   ├── prometheus.rest.yml
│   │   ├── prometheus.grpc.yml
│   │   └── prometheus.kafka.yml
│   └── grafana/
│       └── provisioning/
│           ├── datasources/
│           │   └── prometheus.yml
│           ├── dashboards/
│           │   └── dashboard.yml
│           └── dashboards-json/
│               └── experiment.json
├── k6/
│   ├── smoke-test.js        # Validação rápida do ambiente
│   ├── rest/
│   │   ├── load-test.js     # Carga normal
│   │   └── failure-test.js  # Cenário de falha
│   ├── grpc/
│   │   ├── load-test.js
│   │   └── failure-test.js
│   └── kafka/
│       ├── load-test.js
│       └── failure-test.js
├── scripts/
│   └── failure-scenario.ps1 # Script de interrupção controlada
└── results/
    ├── rest/
    ├── grpc/
    └── kafka/
```

---

## Regras de Execução

> ⚠️ **Importante:** execute apenas **um cenário por vez**.
> Sempre encerre o cenário atual com `docker compose down` antes de iniciar outro.
> Isso garante isolamento total entre as execuções e a validade do experimento.

---

## Portas por Cenário

| Cenário | OrderGateway | OrderProcessor |
|---------|-------------|----------------|
| REST    | `5000`      | `5001`         |
| gRPC    | `5010`      | `5011`         |
| Kafka   | `5020`      | —              |

---

## Portas da Stack de Observabilidade

| Serviço        | URL                   | Credenciais   |
|----------------|-----------------------|---------------|
| Prometheus     | http://localhost:9090  | —             |
| Grafana        | http://localhost:3000  | admin / admin |
| cAdvisor       | http://localhost:8090  | —             |
| Kafka Exporter | http://localhost:9308  | —             |

---

## Limitação de Recursos por Container

| Container       | CPU | Memória |
|-----------------|-----|---------|
| order-gateway   | 1.0 | 256 MB  |
| order-processor | 1.0 | 256 MB  |
| kafka (broker)  | 1.0 | 512 MB  |

---

## Métricas Coletadas

### Infraestrutura (via cAdvisor)
- CPU por container
- Memória por container

### Aplicação (via OpenTelemetry + Prometheus)
- Latência HTTP/gRPC (P95, P99, média)
- Throughput (req/s)
- Taxa de erro

### Domínio (métricas customizadas)
- `orders_processed_total` — total de pedidos processados
- `order_processing_time_ms` — tempo de processamento interno

### Kafka (via kafka-exporter)
- `kafka_consumergroup_lag` — consumer lag por grupo e tópico
- `kafka_topic_partition_current_offset` — offset atual do tópico

---

## Payload Padrão

O mesmo payload é utilizado nos três cenários, garantindo comparabilidade:

```json
{
  "orderId":    "UUID v4",
  "productId":  "string",
  "quantity":   "int",
  "unitPrice":  "decimal",
  "customerId": "string",
  "createdAt":  "ISO 8601"
}
```

---

## Cenário 1 — REST

### 1. Subir o ambiente

```bash
docker compose -f compose/docker-compose.rest.yml up --build
```

### 2. Aguardar os serviços

```
order-processor-rest  | Now listening on: http://[::]:8080
order-gateway-rest    | Now listening on: http://[::]:8080
prometheus-rest       | Server is ready to receive web requests
grafana-rest          | HTTP server listening on :3000
```

### 3. Smoke test — validar ambiente

```bash
k6 run --env BASE_URL=http://localhost:5000 --env EXPECTED_STATUS=200 k6/smoke-test.js
```

### 4. Executar experimento — carga normal

```bash
# Repetição 1
k6 run k6/rest/load-test.js --out json=results/rest/run-1.json

# Repetição 2
k6 run k6/rest/load-test.js --out json=results/rest/run-2.json

# Repetição 3
k6 run k6/rest/load-test.js --out json=results/rest/run-3.json
```

### 5. Executar experimento — cenário de falha

Abrir **dois terminais** em paralelo:

**Terminal 1 — k6:**
```bash
k6 run k6/rest/failure-test.js --out json=results/rest/failure-run-1.json
```

**Terminal 2 — script de falha (executar imediatamente após o k6 iniciar):**
```powershell
.\scripts\failure-scenario.ps1 -Scenario rest
```

> O script aguarda 2 minutos, derruba o Processor por 60 segundos e reinicia.
> Monitore o Grafana em tempo real durante a execução.

### 6. Teste manual via curl

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "productId": "PROD-001",
    "quantity": 2,
    "unitPrice": 49.90,
    "customerId": "CUST-123",
    "createdAt": "2025-01-01T10:00:00Z"
  }'
```

**Resposta esperada:**
```json
{
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "PROCESSED",
  "processedAt": "2025-01-01T10:00:01Z"
}
```

### 7. Encerrar

```bash
docker compose -f compose/docker-compose.rest.yml down
```

---

## Cenário 2 — gRPC

### 1. Subir o ambiente

```bash
docker compose -f compose/docker-compose.grpc.yml up --build
```

### 2. Aguardar os serviços

```
order-processor-grpc  | Now listening on: http://[::]:8080
order-gateway-grpc    | Now listening on: http://[::]:8080
prometheus-grpc       | Server is ready to receive web requests
grafana-grpc          | HTTP server listening on :3000
```

### 3. Smoke test — validar ambiente

```bash
k6 run --env BASE_URL=http://localhost:5010 --env EXPECTED_STATUS=200 k6/smoke-test.js
```

### 4. Executar experimento — carga normal

```bash
# Repetição 1
k6 run k6/grpc/load-test.js --out json=results/grpc/run-1.json

# Repetição 2
k6 run k6/grpc/load-test.js --out json=results/grpc/run-2.json

# Repetição 3
k6 run k6/grpc/load-test.js --out json=results/grpc/run-3.json
```

### 5. Executar experimento — cenário de falha

**Terminal 1 — k6:**
```bash
k6 run k6/grpc/failure-test.js --out json=results/grpc/failure-run-1.json
```

**Terminal 2 — script de falha:**
```powershell
.\scripts\failure-scenario.ps1 -Scenario grpc
```

### 6. Teste manual via curl

> O Gateway gRPC expõe um endpoint HTTP para receber requisições do k6.
> A comunicação gRPC acontece internamente entre Gateway e Processor.

```bash
curl -X POST http://localhost:5010/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "productId": "PROD-001",
    "quantity": 2,
    "unitPrice": 49.90,
    "customerId": "CUST-123",
    "createdAt": "2025-01-01T10:00:00Z"
  }'
```

**Resposta esperada:**
```json
{
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "PROCESSED",
  "processedAt": "2025-01-01T10:00:01Z"
}
```

### 7. Encerrar

```bash
docker compose -f compose/docker-compose.grpc.yml down
```

---

## Cenário 3 — Kafka

### 1. Subir o ambiente

```bash
docker compose -f compose/docker-compose.kafka.yml up --build
```

### 2. Aguardar os serviços

```
kafka                  | Kafka Server started
kafka-exporter         | Starting metrics collection
order-processor-kafka  | Consumer started. Listening on topic orders
order-gateway-kafka    | Now listening on: http://[::]:8080
prometheus-kafka       | Server is ready to receive web requests
grafana-kafka          | HTTP server listening on :3000
```

### 3. Smoke test — validar ambiente

```bash
k6 run --env BASE_URL=http://localhost:5020 --env EXPECTED_STATUS=202 k6/smoke-test.js
```

### 4. Executar experimento — carga normal

```bash
# Repetição 1
k6 run k6/kafka/load-test.js --out json=results/kafka/run-1.json

# Repetição 2
k6 run k6/kafka/load-test.js --out json=results/kafka/run-2.json

# Repetição 3
k6 run k6/kafka/load-test.js --out json=results/kafka/run-3.json
```

### 5. Executar experimento — cenário de falha

**Terminal 1 — k6:**
```bash
k6 run k6/kafka/failure-test.js --out json=results/kafka/failure-run-1.json
```

**Terminal 2 — script de falha:**
```powershell
.\scripts\failure-scenario.ps1 -Scenario kafka
```

> No Kafka, o Gateway continua respondendo 202 mesmo com o Processor down.
> As mensagens acumulam no tópico e são consumidas após o restart.
> Monitore o painel **Consumer Lag** no Grafana durante a falha.

### 6. Teste manual via curl

```bash
curl -X POST http://localhost:5020/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "productId": "PROD-001",
    "quantity": 2,
    "unitPrice": 49.90,
    "customerId": "CUST-123",
    "createdAt": "2025-01-01T10:00:00Z"
  }'
```

**Resposta esperada:**
```json
{
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "ACCEPTED",
  "message": "Order published to Kafka"
}
```

### 7. Encerrar

```bash
docker compose -f compose/docker-compose.kafka.yml down
```

---

## Fluxo de Execução do Experimento

```
Para cada cenário (REST, gRPC, Kafka):

  Carga Normal:
  1. docker compose up --build
  2. Verificar targets → http://localhost:9090/targets
  3. k6 smoke-test
  4. k6 run → run-1.json  (repetição 1)
  5. k6 run → run-2.json  (repetição 2)
  6. k6 run → run-3.json  (repetição 3)
  7. Anotar métricas do Prometheus na planilha
  8. Exportar snapshot do Grafana

  Cenário de Falha:
  9.  Terminal 1 → k6 failure-test → failure-run-1.json
      Terminal 2 → failure-scenario.ps1
  10. Anotar métricas do Prometheus na planilha
  11. Exportar snapshot do Grafana
  12. docker compose down
```

---

## Cenário de Falha — Linha do Tempo

```
t=0:00  → k6 inicia carga (200 VUs)
t=2:00  → OrderProcessor é DERRUBADO
t=3:00  → OrderProcessor é REINICIADO
t=5:00  → k6 encerra

Comportamento esperado por protocolo:
  REST  → erros imediatos, recuperação após restart
  gRPC  → erros imediatos, recuperação após restart
  Kafka → zero erros, consumer lag sobe e zera após restart
```

---

## Coleta de Resultados — Queries do Prometheus

Execute as queries abaixo em http://localhost:9090/graph **antes do `docker compose down`** e anote os valores na planilha.

### Queries — REST e gRPC

```promql
# Latência P95 (ms)
histogram_quantile(0.95, sum(rate(http_server_request_duration_seconds_bucket[5m])) by (le)) * 1000

# Latência P99 (ms)
histogram_quantile(0.99, sum(rate(http_server_request_duration_seconds_bucket[5m])) by (le)) * 1000

# Latência Média (ms)
sum(rate(http_server_request_duration_seconds_sum[5m])) / sum(rate(http_server_request_duration_seconds_count[5m])) * 1000

# Throughput (req/s)
sum(rate(http_server_request_duration_seconds_count[5m]))

# Taxa de erro (%)
sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m])) / sum(rate(http_server_request_duration_seconds_count[5m])) * 100

# Tempo de processamento interno P95 (ms)
histogram_quantile(0.95, sum(rate(order_processing_time_ms_bucket[5m])) by (le))

# Tempo de processamento interno P99 (ms)
histogram_quantile(0.99, sum(rate(order_processing_time_ms_bucket[5m])) by (le))

# CPU Gateway (%)
sum(rate(container_cpu_usage_seconds_total{name=~"order-gateway.*"}[5m])) by (name) * 100

# CPU Processor (%)
sum(rate(container_cpu_usage_seconds_total{name=~"order-processor.*"}[5m])) by (name) * 100

# Memória Gateway (MB)
container_memory_usage_bytes{name=~"order-gateway.*"} / 1024 / 1024

# Memória Processor (MB)
container_memory_usage_bytes{name=~"order-processor.*"} / 1024 / 1024
```

---

### Queries adicionais — Kafka (consumer lag)

```promql
# Consumer lag atual
sum(kafka_consumergroup_lag) by (consumergroup, topic)

# Consumer lag médio durante a execução
avg_over_time(sum(kafka_consumergroup_lag)[5m:])

# Consumer lag máximo (pico — especialmente útil no cenário de falha)
max_over_time(sum(kafka_consumergroup_lag)[5m:])

# Offset atual do tópico (total de mensagens publicadas)
sum(kafka_topic_partition_current_offset) by (topic)
```

> **No cenário de falha:** anote o valor máximo do consumer lag atingido durante
> a indisponibilidade do Processor e o tempo aproximado para o lag zerar após o restart.
> Esses dois valores evidenciam o comportamento de desacoplamento temporal do Kafka.

---

## Dashboard Grafana

O dashboard **TCC — Experimento Microserviços** é provisionado automaticamente e contém:

| Painel | Métrica |
|--------|---------|
| Latência P95 | `histogram_quantile(0.95, ...)` |
| Latência P99 | `histogram_quantile(0.99, ...)` |
| Latência Média | `rate(duration_sum) / rate(duration_count)` |
| Throughput | `rate(http_server_request_duration_seconds_count)` |
| Taxa de Erro | Requisições com status 5xx |
| Pedidos Processados | `orders_processed_total` |
| CPU Gateway | `container_cpu_usage_seconds_total` |
| CPU Processor | `container_cpu_usage_seconds_total` |
| Memória | `container_memory_usage_bytes` |
| Tempo de Processamento Interno P95/P99 | `order_processing_time_ms` |
| Consumer Lag | `kafka_consumergroup_lag` |
| Offset do Tópico | `kafka_topic_partition_current_offset` |

---

## Resultados

```
results/
├── rest/
│   ├── run-1.json
│   ├── run-2.json
│   ├── run-3.json
│   └── failure-run-1.json
├── grpc/
│   ├── run-1.json
│   ├── run-2.json
│   ├── run-3.json
│   └── failure-run-1.json
└── kafka/
    ├── run-1.json
    ├── run-2.json
    ├── run-3.json
    └── failure-run-1.json
```