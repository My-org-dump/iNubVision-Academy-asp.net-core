// iNubVision Academy — shared front-end helpers (same file in Java and .NET versions)
const topbar = document.querySelector('.topbar');
const onScroll = () => topbar && topbar.classList.toggle('scrolled', window.scrollY > 10);
window.addEventListener('scroll', onScroll); onScroll();

if ('IntersectionObserver' in window) {
    const io = new IntersectionObserver(entries => entries.forEach(e => {
        if (!e.isIntersecting) return;
        e.target.classList.add('in');
        e.target.querySelectorAll('[data-count]').forEach(countUp);
        io.unobserve(e.target);
    }), { threshold: 0.12 });
    document.querySelectorAll('.reveal').forEach(el => io.observe(el));
} else {
    document.querySelectorAll('.reveal').forEach(el => el.classList.add('in'));
}

function countUp(el) {
    const target = +el.dataset.count, suffix = el.dataset.suffix || '';
    let n = 0; const step = Math.max(1, Math.round(target / 45));
    const t = setInterval(() => { n = Math.min(target, n + step); el.textContent = n.toLocaleString('en-IN') + suffix; if (n >= target) clearInterval(t); }, 28);
}

// Live-looking pipeline numbers on the landing page
const rps = document.getElementById('rps');
if (rps) setInterval(() => { rps.textContent = (1450 + Math.floor(Math.random() * 220)).toLocaleString('en-IN'); }, 1500);

// Confirm dangerous admin actions
document.addEventListener('submit', e => {
    const msg = e.target.getAttribute('data-confirm');
    if (msg && !confirm(msg)) e.preventDefault();
});
