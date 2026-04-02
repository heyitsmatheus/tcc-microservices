import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.2/index.js';
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate, Counter } from 'k6/metrics';

const latency = new Trend('order_latency', true);
const errorRate = new Rate('order_error_rate');
const orderCount = new Counter('order_count');

export const options = {
    scenarios: {
        light_load: {
            executor: 'constant-vus',
            vus: 50,
            duration: '5m',
            tags: { scenario: 'light_load' },
        },
        moderate_load: {
            executor: 'constant-vus',
            vus: 200,
            duration: '5m',
            startTime: '6m',
            tags: { scenario: 'moderate_load' },
        },
        high_load: {
            executor: 'constant-vus',
            vus: 500,
            duration: '5m',
            startTime: '12m',
            tags: { scenario: 'high_load' },
        },
    },
    thresholds: {
        order_error_rate: ['rate<0.05'],
        // Kafka é assíncrono — latência mede apenas o tempo de publicação
        order_latency: ['p(95)<1000'],
    },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5020';

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

    latency.add(res.timings.duration);
    // Kafka retorna 202 Accepted
    errorRate.add(res.status !== 202);
    orderCount.add(1);

    check(res, {
        'status 202': (r) => r.status === 202,
        'status accepted': (r) => {
            try {
                return JSON.parse(r.body).status === 'ACCEPTED';
            } catch (e) {
                return false;
            }
        },
    });

    sleep(0.1);
}

export function handleSummary(data) {
    return {
        stdout: textSummary(data, { indent: ' ', enableColors: true }),
    };
}