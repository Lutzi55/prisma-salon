// Comprobante de PRUEBA para la demo: dibuja una imagen con el monto, el medio y la fecha,
// marcada como SIMULACIÓN, y la adjunta sola al campo de archivo del formulario.
// No imita a ningún banco real: es solo para poder probar el circuito de pagos sin transferir dinero.
window.ComprobantePrueba = {
    generar: function (cfg) {
        const monto = Number(cfg.monto || 0);
        if (!(monto > 0)) {
            Swal.fire({ icon: 'info', title: 'Falta el monto', text: 'Indicá cuánto pagaste para generar el comprobante.' });
            return;
        }

        const W = 720, H = 980;
        const c = document.createElement('canvas');
        c.width = W; c.height = H;
        const g = c.getContext('2d');
        const verde = '#2B3319', beige = '#E0DDD5', tinta = '#161616', gris = '#5A5A50';
        const serif = '"Marcellus", Georgia, serif', sans = '"Jost", Arial, sans-serif';
        const ahora = new Date();
        const fecha = cfg.fecha ? new Date(cfg.fecha + 'T' + ahora.toTimeString().slice(0, 8)) : ahora;
        const operacion = String(Math.floor(10000000 + Math.random() * 89999999));
        const pesos = '$ ' + monto.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

        // Fondo y cabecera
        g.fillStyle = '#FFFFFF'; g.fillRect(0, 0, W, H);
        g.fillStyle = verde; g.fillRect(0, 0, W, 150);
        g.fillStyle = beige;
        g.font = '38px ' + serif; g.fillText('Comprobante de pago', 48, 78);
        g.font = '20px ' + sans; g.fillText('Generado para la demo de Prisma Salón', 48, 116);

        // Monto
        g.fillStyle = gris; g.font = '20px ' + sans; g.fillText('Monto', 48, 210);
        g.fillStyle = tinta; g.font = '58px ' + serif; g.fillText(pesos, 48, 275);

        // Detalle
        const filas = [
            ['Medio de pago', cfg.metodo || 'Transferencia'],
            ['Fecha y hora', fecha.toLocaleDateString('es-AR') + ' ' + fecha.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' })],
            ['Pagó', cfg.pagador || 'Cliente'],
            ['Destinatario', 'Prisma Salón (alias PRISMA.SALON.EVENTOS)'],
            ['Concepto', cfg.concepto || 'Pago de evento'],
            ['N.º de operación', operacion]
        ];
        if ((cfg.metodo || '').toLowerCase().includes('débito')) filas.splice(1, 0, ['Tarjeta', 'Débito terminada en 4821']);

        let y = 340;
        g.strokeStyle = '#BDB9B0'; g.lineWidth = 1;
        filas.forEach(([k, v]) => {
            g.beginPath(); g.moveTo(48, y - 34); g.lineTo(W - 48, y - 34); g.stroke();
            g.fillStyle = gris; g.font = '19px ' + sans; g.fillText(k, 48, y);
            g.fillStyle = tinta; g.font = '22px ' + sans;
            const texto = String(v).length > 40 ? String(v).slice(0, 39) + '…' : String(v);
            g.fillText(texto, 270, y);
            y += 74;
        });

        // Marca de agua SIMULACIÓN
        g.save();
        g.translate(W / 2, H / 2 + 60);
        g.rotate(-Math.PI / 7);
        g.fillStyle = 'rgba(122, 42, 36, 0.16)';
        g.font = 'bold 110px ' + sans;
        g.textAlign = 'center';
        g.fillText('SIMULACIÓN', 0, 0);
        g.restore();

        // Pie
        g.fillStyle = '#F3F1EC'; g.fillRect(0, H - 110, W, 110);
        g.fillStyle = '#7A2A24'; g.font = 'bold 20px ' + sans;
        g.fillText('Comprobante de prueba, sin validez.', 48, H - 62);
        g.fillStyle = gris; g.font = '18px ' + sans;
        g.fillText('Solo para probar el sistema del Trabajo Final Integrador.', 48, H - 32);

        c.toBlob(blob => {
            const archivo = new File([blob], 'comprobante-prueba-' + operacion + '.png', { type: 'image/png' });
            const dt = new DataTransfer();
            dt.items.add(archivo);
            cfg.input.files = dt.files;
            cfg.input.dispatchEvent(new Event('change', { bubbles: true }));
            if (cfg.preview) {
                cfg.preview.src = URL.createObjectURL(blob);
                cfg.preview.hidden = false;
            }
            Swal.fire({ icon: 'success', title: 'Comprobante generado', text: 'Ya quedó adjunto. Revisalo y enviá el formulario.', timer: 2200, showConfirmButton: false });
        }, 'image/png');
    }
};
