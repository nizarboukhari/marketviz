import { inject } from 'aurelia-framework';
import { OsApiClient } from 'services/os-api-client';

let _endpoint = 'api/game-data/';

@inject(OsApiClient)
export class GameDataManager {

  constructor(apiClient) {
    this._apiClient = apiClient;
  }

  getLatest() {
    let _url = _endpoint;

    return this._apiClient
      .get({
        url: _url + 'lastest/'
      })
      .then(response => response.json());
  }

}
