// Cálculo del total (paquete + opciones) y regla de "una sola opción" por categoría.
// Lo usan el formulario de contratos del administrador y "Personalizar paquete" del cliente.
window.Personalizar = {
    iniciar: function (cfg) {
        const form = cfg.form;
        if (!form) return;
        const formato = n => '$ ' + Math.round(n).toLocaleString('es-AR');

        function precioPaquete() {
            if (cfg.precioBase !== undefined) return cfg.precioBase;
            const op = cfg.paquete && cfg.paquete.selectedOptions[0];
            return op && op.dataset.precio ? parseFloat(op.dataset.precio) : 0;
        }

        function recalcular() {
            let total = precioPaquete();
            let extras = 0;
            form.querySelectorAll('input[name="OpcionesIds"]:checked').forEach(c => {
                extras += parseFloat(c.dataset.precio || '0');
            });
            total += extras;
            if (cfg.total) cfg.total.textContent = formato(total);
            if (cfg.extras) cfg.extras.textContent = extras > 0 ? '+ ' + formato(extras) : 'Sin costo extra';
            if (cfg.sena) cfg.sena.textContent = formato(total * (cfg.porcentajeSena || 30) / 100);
            if (cfg.alCambiar) cfg.alCambiar(total, formato);
            return total;
        }

        // En las categorías de opción única, al marcar una se desmarcan las demás
        form.querySelectorAll('.grupo-opc[data-unica="1"]').forEach(grupo => {
            grupo.addEventListener('change', e => {
                if (e.target.name !== 'OpcionesIds' || !e.target.checked) return;
                grupo.querySelectorAll('input[name="OpcionesIds"]').forEach(c => { if (c !== e.target) c.checked = false; });
            });
        });

        form.addEventListener('change', recalcular);
        if (cfg.alUsar && cfg.alUsar.boton) {
            cfg.alUsar.boton.addEventListener('click', () => { cfg.alUsar.campo.value = recalcular().toFixed(2); });
        }
        recalcular();
    }
};
