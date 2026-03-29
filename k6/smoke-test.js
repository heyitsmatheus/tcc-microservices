import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 1,
    duration: '30s',
    thresholds: {
        http_req_failed: ['rate<0.01'],   // menos de 1% de erro
        http_req_duration: ['p(95)<2000'],  // P95 abaixo de 2s
    },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// Kafka retorna 202, REST e gRPC retornam 200
const EXPECTED_STATUS = parseInt(__ENV.EXPECTED_STATUS || '200');

const PAYLOAD = JSON.stringify({
    orderId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
    productId: 'PROD-001',
    quantity: 2,
    unitPrice: 49.90,
    customerId: 'CUST-123',
    createdAt: '2025-01-01T10:00:00Z',
});

const HEADERS = { 'Content-Type': 'application/json' };

export default function () {
    const res = http.post(`${BASE_URL}/api/orders`, PAYLOAD, { headers: HEADERS });

    check(res, {
        [`status ${EXPECTED_STATUS}`]: (r) => r.status === EXPECTED_STATUS,
        'response time < 2s': (r) => r.timings.duration < 2000,
        'body not empty': (r) => r.body && r.body.length > 0,
    });

    sleep(1);
}