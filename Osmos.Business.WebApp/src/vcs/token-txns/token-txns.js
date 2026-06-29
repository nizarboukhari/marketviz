import { inject } from 'aurelia-framework';
import { TxnsManager } from 'managers/txnsManager';

@inject(TxnsManager)
export class TokensList {

  constructor(txnsManager) {
    this._txnsManager = txnsManager;

    this.tokenAddress = null;

    this.txns = [];
  }

  attached() {

    this.getTokenTxns();
    this.interval = setInterval(() => {
      this.getTokenTxns();
    }, 3000);
  }

  detached(){
    clearInterval(this.interval);
  }

  getTokenTxns() {
    if(!this.tokenAddress || !this.tokenAddress.length) return Promise.resolve();

    this.loading = true;
    return this._txnsManager.getTokenTxns(this.tokenAddress)
      .then(result => {
        this.txns = result;
        console.info('this.txns ', this.txns );
      })
      .catch(error => {
        console.error('getTokenTxns', error);
      });
  }

}
