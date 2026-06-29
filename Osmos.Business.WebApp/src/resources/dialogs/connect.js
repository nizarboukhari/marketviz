import { inject } from 'aurelia-framework';
import { DialogController } from 'aurelia-dialog';

@inject(DialogController)
export class Connect {

  constructor(controller) {
    this.controller = controller;
  }

  ok() {
    this.controller.ok({});
  }

  openBuyViz(){
    window.open('https://app.uniswap.org/#/swap?outputCurrency=0x2C10c0dE3362FF21F8ED6bC7F4AC5e391153fD2c&chain=mainnet', '_blank');
  }

}
