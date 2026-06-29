import { inject } from 'aurelia-framework';
import { OsApiClient } from 'services/os-api-client';
import queryString from 'query-string';

let _endpoint = 'api/txns/';

@inject(OsApiClient)
export class TxnsManager {

  constructor(apiClient) {
    this._apiClient = apiClient;
  }

  getTxnsByBlock() {
    let _url = _endpoint + 'by-block/';

    return this._apiClient
      .get({
        url: _url
      })
      .then(response => response.json());
  }

  getTxnsByToken() {
    let _url = _endpoint + 'by-token/';

    return this._apiClient
      .get({
        url: _url
      })
      .then(response => response.json());
  }

  getTokenTxns(address, limit) {
    let _url = _endpoint + 'token/' + address;
    if (limit) _url += `?limit=${limit}`;

    return this._apiClient
      .get({
        url: _url
      })
      .then(response => response.json());
  }

  getTokensTxns(addresses, limit) {
    let _url = _endpoint + 'tokens/';

    const qs = queryString.stringify({
      addresses: addresses,
      limit: limit
    });
    if (qs && qs.length) {
      _url += `?${qs}`;
    }

    return this._apiClient
      .get({
        url: _url
      })
      .then(response => response.json());
  }

  getOwnerTxns(address, limit) {
    let _url = _endpoint + 'owner/' + address;
    if (limit) _url += `?limit=${limit}`;

    return this._apiClient
      .get({
        url: _url
      })
      .then(response => response.json());
  }

}
