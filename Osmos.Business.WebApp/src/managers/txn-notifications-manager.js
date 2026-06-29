import { inject } from 'aurelia-framework';
import { OsApiClient } from 'services/os-api-client';
import queryString from 'query-string';

let _endpoint = 'api/txn-notifications/';

@inject(OsApiClient)
export class TxnNotificationsManager {

  constructor(apiClient) {
    this._apiClient = apiClient;
  }

  getTxnNotifications(){
    let _url = _endpoint;

    return this._apiClient
      .get({
        url: _url
      })
      .then(response => response.json());
  }


}
