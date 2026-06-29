import { inject, bindable } from 'aurelia-framework';
import environment from '../../config/environment.json';
import { HttpClient, json } from '@aurelia/fetch-client';
import { OsGuid } from 'services/os-guid';
import { Router } from 'aurelia-router';
import { EventAggregator } from 'aurelia-event-aggregator';

let _apiUrl = '';
let _client = new HttpClient();

@inject(Router, EventAggregator)
export class OsApiClient {

  @bindable currentRequests = [];

  constructor(router, eventAggregator) {

    this._router = router;
    this._eventAggregator = eventAggregator;

    if (environment.apiUrl) {
      _apiUrl = environment.apiUrl;
    }

    this.requesting = false;
  }

  currentRequestsChanged(){
    this.requesting = !!this.currentRequests.length;
  }

  get(options) {
    let o = options || {};
    o.method = 'get';

    return this.execute(o);
  }

  post(options) {
    let o = options || {};
    o.method = 'post';

    return this.execute(o);
  }

  put(options) {
    let o = options || {};
    o.method = 'put';

    return this.execute(o);
  }

  delete(options) {
    let o = options || {};
    o.method = 'delete';

    return this.execute(o);
  }

  execute(options) {
    let o = {
      method: options && options.method ? options.method : 'get',
      headers: options && options.headers ? options.headers : {},
      body: options && options.data ? json(options.data) : null,
      apiUrl: options && options.apiUrl ? options.apiUrl : _apiUrl
    };

    let overrideHeaders = !!options.overrideHeaders;

    if (options && options.formData) {
      o.body = options.formData;
    }

    if (options && options.headers && options.headers['Content-Type']) {
      o.body = options.data;
    }

    let token = localStorage.getItem('token');
    if (token && !overrideHeaders) {
      o.headers['Authorization'] = `Bearer ${token}`;
    }

    let wallet = localStorage.getItem('wallet');
    if (wallet && !overrideHeaders) {
      o.headers['wallet'] = `${wallet}`;
    }

    let identityId = localStorage.getItem('identityId');
    if (identityId && !overrideHeaders) {
      o.headers['os-identity-id'] = identityId;
    }

    let url = options && options.url ? options.url : '';
    url = options.fullUrl || o.apiUrl + url;
    let apiRequest = this._addRequest(url, o.method);

    let promise = new Promise((resolve, reject) => {
      _client.fetch(url, o)
        .then(response => {
          this._removeRequest(apiRequest.id);
          if (response.status == 401) {
            this._eventAggregator.publish('logout');
          }
          if (response && !response.ok) {
            return response.text().then((text) => {
              reject(new Error(JSON.stringify({
                status: response.status,
                body: text
              })));
            });
          }
          resolve(response);
        })
        .catch(error => {
          this._removeRequest(apiRequest.id);
          reject(error);
        });
    });

    return promise;
  }

  downloadToBlob(options, onprogress) {

    let promise = new Promise((resolve, reject) => {
      if (!options || !options.url || !options.url.length) reject('bad xhr arguments');

      let xhr = new XMLHttpRequest();
      xhr.open('GET', _apiUrl + options.url, true);
      xhr.responseType = 'blob';

      let token = localStorage.getItem('token');
      if (token) {
        options.headers = {
          'Authorization': `Bearer ${token}`
        };
      }
      if (options.headers) {
        Object.keys(options.headers).forEach(key => {
          xhr.setRequestHeader(key, options.headers[key]);
        });
      }

      xhr.onload = (event) => {
        options.xhr = null;
        if (xhr.status >= 200 && xhr.status < 300) {
          let result = URL.createObjectURL(xhr.response);
          resolve(result);
        }
        else {
          console.info('1st reject:', xhr);
          reject(new Error(xhr.statusText));
        }
      };

      xhr.onerror = (error, x) => {
        options.xhr = null;
        console.info('2nd reject:', error, x, xhr);
        reject(new Error(xhr.statusText));
      };

      xhr.onprogress = onprogress;

      // xhr.onprogress = (event) => {
      //     if (event.lengthComputable) {
      //         ref.loadedBytes = event.loaded;
      //         ref.size = event.total;
      //     }
      // };

      options.xhr = xhr;

      xhr.send();
    });

    return promise;
  }

  _addRequest(url, method) {
    let apiRequest = new ApiRequest(url, method);
    this.currentRequests.push(apiRequest);
    this.currentRequestsChanged();
    return apiRequest;
  }

  _removeRequest(id) {
    this.currentRequests = this.currentRequests.filter(item => item.id !== id);
    this.currentRequestsChanged();
  }
}

class ApiRequest {
  constructor(url, method) {
    this.id = OsGuid.New();
    this.url = url;
    this.method = method;
  }
}
