import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate, Counter } from 'k6/metrics';

const latency = new Trend('order_latency', true);
const errorRate = new Rate('order_error_rate');
const orderCount = new Counter('order_count');
const errorCount = new Counter('order_error_count');

export const options = {
    vus: 200,
    duration: '5m',
    thresholds: {
        // No cenário de falha os thresholds são informativos
        // não bloqueantes — queremos ver todos os dados
        order_error_rate: ['rate<0.5'],
    },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

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
    const res = http.post(`${BASE_URL}/api/orders`, PAYLOAD, {
        headers: HEADERS,
        timeout: '10s', // timeout generoso para capturar latência alta
    });

    const success = res.status === 200 || res.status === 202;

    latency.add(res.timings.duration);
    errorRate.add(!success);
    orderCount.add(1);

    if (!success) {
        errorCount.add(1);
        console.log(`ERROR at ${new Date().toISOString()} — status: ${res.status}`);
    }

    check(res, {
        'success': (r) => r.status === 200 || r.status === 202,
    });

    sleep(0.1);
}