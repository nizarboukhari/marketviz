import { inject } from 'aurelia-framework';
import { OsApiClient } from 'services/os-api-client';

let _endpoint = 'api/data-infos/';

@inject(OsApiClient)
export class DataInfosManager {

  constructor(apiClient) {
    this._apiClient = apiClient;
  }

  getDataInfos() {
    let _url = _endpoint;

    return this._apiClient
      .get({
        url: _url
      })
      .then(response => response.json());
  }

}
