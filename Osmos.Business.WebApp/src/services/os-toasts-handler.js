import toastr from 'toastr';

export class ToastsHandler {
  constructor() {
    toastr.options = {
      "closeButton": true,
      "debug": false,
      "newestOnTop": false,
      "progressBar": false,
      "positionClass": "toast-bottom-left",
      "preventDuplicates": true,
      "onclick": null,
      "showDuration": "300",
      "hideDuration": "1000",
      "timeOut": "9000",
      "extendedTimeOut": "1000",
      "showEasing": "swing",
      "hideEasing": "linear",
      "showMethod": "fadeIn",
      "hideMethod": "fadeOut",
      "escapeHtml": false
    };
  }

  success(message) {
    toastr.success(message);
  }

  error(error) {
    toastr.error(error);
  }

  info(message) {
    toastr.info(message);
  }

  warning(message) {
    toastr.warning(message);
  }
}
