// Confirmación antes de eliminar: cualquier formulario con la clase "form-eliminar"
// muestra un modal de SweetAlert2 y recién envía si el usuario confirma.
document.addEventListener('submit', function (e) {
    const form = e.target;
    if (!form.classList || !form.classList.contains('form-eliminar') || form.dataset.confirmado) return;
    e.preventDefault();

    Swal.fire({
        icon: 'warning',
        title: '¿Eliminar este registro?',
        text: form.dataset.mensaje || 'Esta acción no se puede deshacer.',
        showCancelButton: true,
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar',
        reverseButtons: true,
        focusCancel: true
    }).then(function (r) {
        if (r.isConfirmed) {
            form.dataset.confirmado = '1';
            form.submit();
        }
    });
});

// Ver un comprobante sin salir de la página: se abre en un modal en vez de otra pestaña
document.addEventListener('click', function (e) {
    const a = e.target.closest && e.target.closest('a[data-ver-comprobante]');
    if (!a) return;
    e.preventDefault();
    Swal.fire({
        title: 'Comprobante',
        imageUrl: a.href,
        imageAlt: 'Comprobante de pago',
        width: 640,
        showCloseButton: true,
        confirmButtonText: 'Cerrar',
        footer: '<a href="' + a.href + '" download>Descargar imagen</a>'
    });
});
