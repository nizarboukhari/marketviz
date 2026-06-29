import { inject } from 'aurelia-framework';
import { TxnsManager } from 'managers/txnsManager';

@inject(TxnsManager)
export class TokensList {

  constructor(txnsManager) {
    this._txnsManager = txnsManager;

    this.tokens = [];
  }

  attached() {

    this.getTxnsByToken();
    this.interval = setInterval(() => {
      this.getTxnsByToken();
    }, 3000);
  }

  detached(){
    clearInterval(this.interval);
  }

  getTxnsByToken() {
    this.loading = true;
    return this._txnsManager.getTxnsByToken()
      .then(result => {
        this.tokens = result;
        console.info('this.tokens ', this.tokens );
      })
      .catch(error => {
        console.error('getTxnsByToken', error);
      });
  }

}
