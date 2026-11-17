// Animaciones del sitio. La secuencia de entrada del inicio es solo CSS;
// acá se activan las que dependen del scroll y la consulta de fecha del inicio.
(function () {
    // Elementos con data-animar: se marcan como visibles al entrar en pantalla (una sola vez)
    const animables = document.querySelectorAll('[data-animar]');
    if ('IntersectionObserver' in window) {
        const obs = new IntersectionObserver(entradas => {
            entradas.forEach(e => {
                if (e.isIntersecting) {
                    e.target.classList.add('visible');
                    obs.unobserve(e.target);
                }
            });
        }, { threshold: 0.35 });
        animables.forEach(el => obs.observe(el));
    } else {
        animables.forEach(el => el.classList.add('visible'));
    }

    // "¿Ya tenés fecha?": arma la pregunta y se la pasa al asistente
    const form = document.getElementById('consultaFecha');
    if (form) {
        const campo = form.querySelector('input[type=date]');
        form.addEventListener('submit', e => {
            e.preventDefault();
            if (!campo.value) { campo.focus(); return; }
            const [a, m, d] = campo.value.split('-');
            const pregunta = `¿Está libre el ${d}/${m}/${a} para hacer un evento?`;
            if (window.prismaChat) window.prismaChat.preguntar(pregunta);
        });
    }
})();
