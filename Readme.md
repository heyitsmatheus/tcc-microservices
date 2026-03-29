# TCC — Comparação de Mecanismos de Comunicação entre Microserviços

Experimento controlado comparando **REST**, **gRPC** e **Kafka** como mecanismos de comunicação entre microserviços.

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Docker Compose](https://docs.docker.com/compose/)

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
├── k6/                  # Scripts de carga
└── results/             # Resultados dos experimentos
```

---

## Regras de Execução

> ⚠️ **Importante:** execute apenas **um cenário por vez**.
> Sempre encerre o cenário atual com `docker compose down` antes de iniciar outro.
> Isso garante isolamento total entre as execuções e a validade do experimento.

---

## Cenário 1 — REST

### Subir o ambiente

```bash
docker compose -f compose/docker-compose.rest.yml up --build
```

### Aguardar os serviços

```
order-processor-rest  | Now listening on: http://[::]:8080
order-gateway-rest    | Now listening on: http://[::]:8080
```

### Testar

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

### Resposta esperada

```json
{
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "PROCESSED",
  "processedAt": "2025-01-01T10:00:01Z"
}
```

### Encerrar

```bash
docker compose -f compose/docker-compose.rest.yml down
```

---

## Cenário 2 — gRPC

### Subir o ambiente

```bash
docker compose -f compose/docker-compose.grpc.yml up --build
```

### Aguardar os serviços

```
order-processor-grpc  | Now listening on: http://[::]:8080
order-gateway-grpc    | Now listening on: http://[::]:8080
```

### Testar

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

### Resposta esperada

```json
{
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "PROCESSED",
  "processedAt": "2025-01-01T10:00:01Z"
}
```

### Encerrar

```bash
docker compose -f compose/docker-compose.grpc.yml down
```

---

## Cenário 3 — Kafka

### Subir o ambiente

```bash
docker compose -f compose/docker-compose.kafka.yml up --build
```

### Aguardar os serviços

```
kafka                  | Kafka Server started
order-processor-kafka  | Consumer started. Listening on topic orders
order-gateway-kafka    | Now listening on: http://[::]:8080
```

### Testar

> O Gateway publica a mensagem no tópico Kafka e responde imediatamente com HTTP 202 Accepted.
> O Processor consome a mensagem de forma assíncrona — sem resposta direta ao Gateway.

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

### Resposta esperada

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

### Encerrar

```bash
docker compose -f compose/docker-compose.kafka.yml down
```

---

## Portas por Cenário

| Cenário | OrderGateway | OrderProcessor |
|---------|-------------|----------------|
| REST    | `5000`      | `5001`         |
| gRPC    | `5010`      | `5011`         |
| Kafka   | `5020`      | —              |

---

## Limitação de Recursos por Container

| Container         | CPU  | Memória |
|-------------------|------|---------|
| order-gateway     | 1.0  | 256 MB  |
| order-processor   | 1.0  | 256 MB  |
| kafka (broker)    | 1.0  | 512 MB  |

---

## Payload Padrão

O mesmo payload é utilizado nos três cenários, garantindo comparabilidade:

```json
{
  "orderId": "UUID v4",
  "productId": "string",
  "quantity": "int",
  "unitPrice": "decimal",
  "customerId": "string",
  "createdAt": "ISO 8601"
}
```

---

## Execução dos Experimentos com k6

> Documentação dos scripts k6 será adicionada nesta seção.

---

## Resultados

Os resultados de cada execução são salvos em:

```
results/
├── rest/
├── grpc/
└── kafka/
```