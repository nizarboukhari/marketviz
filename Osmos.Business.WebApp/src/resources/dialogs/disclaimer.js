import { inject } from 'aurelia-framework';
import { DialogController } from 'aurelia-dialog';

@inject(DialogController)
export class Disclaimer {

  constructor(controller) {
    this.controller = controller;
  }

  ok() {
    this.controller.ok({});
  }
}
