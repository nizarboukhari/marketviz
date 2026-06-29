import { inject } from 'aurelia-framework';
import { OsApiClient } from 'services/os-api-client';

let _endpoint = 'api/tokens/';

@inject(OsApiClient)
export class TokensManager {

  constructor(apiClient) {
    this._apiClient = apiClient;
  }

  getTokens() {
    let _url = _endpoint;

    return this._apiClient
      .get({
        url: _url
      })
      .then(response => response.json());
  }

  getToken(address) {
    let _url = _endpoint;

    return this._apiClient
      .get({
        url: _url + address
      })
      .then(response => response.json());
  }
  
  getTrending(){
    let _url = _endpoint + 'trending';

    return this._apiClient
      .get({
        url: _url
      })
      .then(response => response.json());
  }

  getOpportunity(){
    let _url = _endpoint + 'opportunity';

    return this._apiClient
      .get({
        url: _url
      })
      .then(response => response.json());
  }

  getScore(){
    let _url = _endpoint + 'score';

    return this._apiClient
      .get({
        url: _url
      })
      .then(response => response.json());
  }

}
