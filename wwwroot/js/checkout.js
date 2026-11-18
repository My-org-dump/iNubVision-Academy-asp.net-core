// Razorpay checkout. The page provides window.RZP = { key, orderId, amount, name, email, phone, brand, verifyUrl, csrfName, csrfToken }
(function () {
    const btn = document.getElementById('pay-btn');
    if (!btn || !window.RZP) return;
    const c = window.RZP;

    function postVerify(resp) {
        const f = document.createElement('form');
        f.method = 'post';
        f.action = c.verifyUrl;
        const add = (n, v) => { const i = document.createElement('input'); i.type = 'hidden'; i.name = n; i.value = v; f.appendChild(i); };
        add('razorpay_payment_id', resp.razorpay_payment_id);
        add('razorpay_order_id', resp.razorpay_order_id);
        add('razorpay_signature', resp.razorpay_signature);
        if (c.csrfName) add(c.csrfName, c.csrfToken);
        document.body.appendChild(f);
        f.submit();
    }

    btn.addEventListener('click', e => {
        e.preventDefault();
        const rzp = new Razorpay({
            key: c.key,
            amount: c.amount,
            currency: 'INR',
            name: c.brand,
            description: 'Course enrollment',
            order_id: c.orderId,
            prefill: { name: c.name, email: c.email, contact: c.phone || '' },
            theme: { color: '#7c3aed' },
            handler: postVerify,
            modal: { ondismiss: () => { btn.disabled = false; } }
        });
        rzp.on('payment.failed', r => {
            document.getElementById('pay-msg').textContent = 'Payment failed: ' + (r.error && r.error.description || 'please try again');
        });
        btn.disabled = true;
        rzp.open();
    });
})();
