import { inject } from 'aurelia-framework';
import { OsApiClient } from 'services/os-api-client';

let _endpoint = 'api/promoted-tokens/';

@inject(OsApiClient)
export class PromotedTokensManager {

  constructor(apiClient) {
    this._apiClient = apiClient;
    
    this.promotedTokens = [];
  }

  getPromotedTokens() {
    let _url = _endpoint;

    return this._apiClient
      .get({
        url: _url
      })
      .then(response => response.json())
      .then(promotedTokens => {
        this.promotedTokens = promotedTokens;
        console.info('this.promotedTokens', this.promotedTokens);
        return this.promotedTokens;
      });
  }

}
