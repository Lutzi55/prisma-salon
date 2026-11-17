// Chat flotante de Prisma Salón: envía la consulta al servidor (/Chat/Preguntar),
// que es quien llama a Gemini. La clave de la API nunca llega al navegador.
(function () {
    const raiz = document.getElementById('chat');
    if (!raiz) return;

    const panel = document.getElementById('chatPanel');
    const abrir = document.getElementById('chatAbrir');
    const cerrar = document.getElementById('chatCerrar');
    const form = document.getElementById('chatForm');
    const texto = document.getElementById('chatTexto');
    const lista = document.getElementById('chatMensajes');
    const sugerencias = document.getElementById('chatSugerencias');
    const token = form.querySelector('input[name="__RequestVerificationToken"]').value;

    const historial = [];
    let ocupado = false;

    function mostrar(abierto) {
        panel.hidden = !abierto;
        abrir.setAttribute('aria-expanded', abierto);
        abrir.classList.toggle('abierto', abierto);
        if (abierto) texto.focus();
    }

    abrir.addEventListener('click', () => mostrar(panel.hidden));
    cerrar.addEventListener('click', () => mostrar(false));
    document.addEventListener('keydown', e => { if (e.key === 'Escape' && !panel.hidden) mostrar(false); });

    function escapar(t) {
        return t.replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }

    // Formato mínimo y seguro: primero se escapa todo, después **negrita**, viñetas y saltos de línea
    function formato(t) {
        return escapar(t)
            .replace(/\*\*(.+?)\*\*/g, '<b>$1</b>')
            .replace(/^\s*[-*]\s+/gm, '• ')
            .replace(/\n/g, '<br>');
    }

    function agregar(contenido, clase) {
        const div = document.createElement('div');
        div.className = 'msg ' + clase;
        div.innerHTML = clase.startsWith('bot') ? formato(contenido) : escapar(contenido);
        lista.appendChild(div);
        lista.scrollTop = lista.scrollHeight;
        return div;
    }

    async function enviar(mensaje) {
        mensaje = (mensaje || '').trim();
        if (!mensaje || ocupado) return;
        ocupado = true;

        agregar(mensaje, 'usuario');
        texto.value = '';
        if (sugerencias) sugerencias.hidden = true;
        const espera = agregar('Escribiendo…', 'bot espera');

        try {
            const r = await fetch(raiz.dataset.url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                body: JSON.stringify({ mensaje: mensaje, historial: historial.slice(-8) })
            });
            const datos = await r.json().catch(() => ({}));
            // 400 sin respuesta: el token de seguridad de la página venció (por ejemplo, después de mucho tiempo abierta)
            const respuesta = datos.respuesta || (r.status === 400
                ? 'La página estuvo abierta mucho tiempo. Recargala para seguir chateando.'
                : 'No pude responder en este momento. Probá de nuevo.');
            espera.remove();
            agregar(respuesta, r.ok ? 'bot' : 'bot error');
            if (r.ok) historial.push({ rol: 'user', texto: mensaje }, { rol: 'model', texto: respuesta });
        } catch {
            espera.remove();
            agregar('No hay conexión con el asistente. Probá de nuevo.', 'bot error');
        } finally {
            ocupado = false;
            if (sugerencias) sugerencias.hidden = false; // vuelven a aparecer las preguntas sugeridas
            lista.scrollTop = lista.scrollHeight;
            texto.focus();
        }
    }

    form.addEventListener('submit', e => { e.preventDefault(); enviar(texto.value); });
    if (sugerencias)
        sugerencias.querySelectorAll('button').forEach(b => b.addEventListener('click', () => enviar(b.textContent)));

    // Otros botones de la página pueden abrir el chat (atributo data-abrir-chat)
    document.querySelectorAll('[data-abrir-chat]').forEach(b => b.addEventListener('click', () => mostrar(true)));

    // API mínima para otros scripts (por ejemplo, la consulta de fecha del inicio)
    window.prismaChat = {
        abrir: () => mostrar(true),
        preguntar: mensaje => { mostrar(true); enviar(mensaje); }
    };
})();
