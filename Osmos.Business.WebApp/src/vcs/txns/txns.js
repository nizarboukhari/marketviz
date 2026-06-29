import { inject } from 'aurelia-framework';
import { TxnsManager } from 'managers/txnsManager';

@inject(TxnsManager)
export class Txns {

  constructor(txnsManager) {
    this._txnsManager = txnsManager;

    this.blocks = [];
  }

  attached() {

    this.getTxnsByBlock();
    this.interval = setInterval(() => {
      this.getTxnsByBlock();
    }, 3000);
  }

  detached(){
    clearInterval(this.interval);
  }

  getTxnsByBlock() {
    this.loading = true;
    return this._txnsManager.getTxnsByBlock()
      .then(result => {
        this.blocks = result;
        console.info('this.blocks ', this.blocks );
      })
      .catch(error => {
        console.error('getTxnsByBlock', error);
      });
  }

}
