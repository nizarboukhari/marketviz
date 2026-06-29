import { inject } from 'aurelia-framework';
import { OsApiClient } from 'services/os-api-client';

let _endpoint = 'api/hot-tokens/';

@inject(OsApiClient)
export class HotTokensManager {
  constructor(apiClient) {
    this._apiClient = apiClient;

    this.hotTokens = [];
  }

  getHotTokens() {
    let _url = _endpoint;

    return this._apiClient
      .get({
        url: _url
      })
      .then(response => response.json())
      .then(hotTokens => {
        this.hotTokens = hotTokens;
        return this.hotTokens;
      });
  }
}
