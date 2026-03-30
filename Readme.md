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
│   ├── smoke-test.js    # Validação rápida do ambiente
│   ├── rest/
│   │   └── load-test.js
│   ├── grpc/
│   │   └── load-test.js
│   └── kafka/
│       └── load-test.js
└── results/             # Resultados dos experimentos
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

| Serviço    | URL                         | Credenciais  |
|------------|-----------------------------|--------------|
| Prometheus | http://localhost:9090        | —            |
| Grafana    | http://localhost:3000        | admin / admin|
| cAdvisor   | http://localhost:8090        | —            |

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

### 4. Executar experimento

```bash
# Repetição 1
k6 run k6/rest/load-test.js --out json=results/rest/run-1.json

# Repetição 2
k6 run k6/rest/load-test.js --out json=results/rest/run-2.json

# Repetição 3
k6 run k6/rest/load-test.js --out json=results/rest/run-3.json
```

### 5. Teste manual via curl

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

### 6. Encerrar

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

### 4. Executar experimento

```bash
# Repetição 1
k6 run k6/grpc/load-test.js --out json=results/grpc/run-1.json

# Repetição 2
k6 run k6/grpc/load-test.js --out json=results/grpc/run-2.json

# Repetição 3
k6 run k6/grpc/load-test.js --out json=results/grpc/run-3.json
```

### 5. Teste manual via curl

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

### 6. Encerrar

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
order-processor-kafka  | Consumer started. Listening on topic orders
order-gateway-kafka    | Now listening on: http://[::]:8080
prometheus-kafka       | Server is ready to receive web requests
grafana-kafka          | HTTP server listening on :3000
```

### 3. Smoke test — validar ambiente

```bash
k6 run --env BASE_URL=http://localhost:5020 --env EXPECTED_STATUS=202 k6/smoke-test.js
```

### 4. Executar experimento

```bash
# Repetição 1
k6 run k6/kafka/load-test.js --out json=results/kafka/run-1.json

# Repetição 2
k6 run k6/kafka/load-test.js --out json=results/kafka/run-2.json

# Repetição 3
k6 run k6/kafka/load-test.js --out json=results/kafka/run-3.json
```

### 5. Teste manual via curl

> O Gateway publica a mensagem no tópico Kafka e responde imediatamente com HTTP 202 Accepted.
> O Processor consome a mensagem de forma assíncrona.

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

> O log do `order-processor-kafka` deve exibir:
> ```
> info: Processing order 3fa85f64-5717-4562-b3fc-2c963f66afa6
> ```

### 6. Encerrar

```bash
docker compose -f compose/docker-compose.kafka.yml down
```

---

## Fluxo de Execução do Experimento

```
Para cada cenário (REST, gRPC, Kafka):

  1. docker compose up --build
  2. Verificar targets no Prometheus → http://localhost:9090/targets
  3. k6 smoke-test          ← valida ambiente
  4. k6 run → run-1.json    ← repetição 1
  5. k6 run → run-2.json    ← repetição 2
  6. k6 run → run-3.json    ← repetição 3
  7. Exportar snapshots do Grafana
  8. docker compose down
```

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

---

## Resultados

Os resultados de cada execução são salvos em:

```
results/
├── rest/
│   ├── run-1.json
│   ├── run-2.json
│   └── run-3.json
├── grpc/
│   ├── run-1.json
│   ├── run-2.json
│   └── run-3.json
└── kafka/
    ├── run-1.json
    ├── run-2.json
    └── run-3.json
```