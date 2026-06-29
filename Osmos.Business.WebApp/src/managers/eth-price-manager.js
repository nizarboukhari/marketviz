import { inject } from 'aurelia-framework';
import { OsApiClient } from 'services/os-api-client';
import { EventAggregator } from 'aurelia-event-aggregator';

@inject(OsApiClient, EventAggregator)
export class EthPriceManager {

  constructor(apiClient, eventAggregator) {
    this._apiClient = apiClient;
    this._eventAggregator = eventAggregator;

    this.ethPrice = null;
  }

  get() {
    return this._apiClient
      .get({
        fullUrl: 'https://min-api.cryptocompare.com/data/price?fsym=ETH&tsyms=USD',
        overrideHeaders: true
      })
      .then(response => response.json())
      .then(result => {
        this.ethPrice = result['USD'];
        this._eventAggregator.publish('eth-price-changed', this.ethPrice);
        return this.ethPrice;
      });
  }

}
