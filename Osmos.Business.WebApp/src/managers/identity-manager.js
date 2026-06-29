import { OsApiClient } from 'services/os-api-client';
import { inject } from 'aurelia-framework';
import { EventAggregator } from 'aurelia-event-aggregator';
import { ToastsHandler } from 'services/os-toasts-handler';

let _endpoint = 'api/identity/';

@inject(OsApiClient, EventAggregator, ToastsHandler)
export class IdentityManager {
  constructor(apiClient, eventAggregator, toastsHandler) {
    this._apiClient = apiClient;
    this._eventAggregator = eventAggregator;
    this._toastsHandler = toastsHandler;

    this.userInfo = null;

    this._eventAggregator.subscribe('logout', () => this.logout());
  }

  login(walletAddress) {
    return this._apiClient
      .post({
        url: 'api/identity/login',
        data: {
          walletAddress: walletAddress
        }
      })
      .then(async response => {
        return response.json().then(result => {
          localStorage.setItem('token', result.accessToken);
          localStorage.setItem('wallet', walletAddress);
          return this.getUserInfo();
        });
      })
      .catch(error => {
        console.error('login', error);
        try {
          const parsed = JSON.parse(error.message);
          if (parsed.status == 400) {
            this._toastsHandler.error(parsed.body);
          }
        } catch (e) {
          console.error('error parsing login error object');
        }
      });
  }

  logout(){
    localStorage.removeItem('token');
    localStorage.removeItem('wallet');
    this.userInfo = null;
    this._userInfoHasChanged();
  }

  getUserInfo() {
    return this._apiClient
      .get({
        url: _endpoint + 'userInfo'
      })
      .then(response => response.json())
      .then(result => {
        this.userInfo = result;
        console.info('userInfo', this.userInfo);
        this._userInfoHasChanged();
        return result;
      })
      .catch(error => {
        this.userInfo = null;
        this._userInfoHasChanged();
        return null;
      });
  }

  _userInfoHasChanged() {
    this._eventAggregator.publish('userinfo-changed', this.userInfo);
  }
}
