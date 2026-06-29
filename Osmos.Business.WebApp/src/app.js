import { inject, bindable } from 'aurelia-framework';
import { DialogService } from 'aurelia-dialog';
import { EventAggregator } from 'aurelia-event-aggregator';
import { AppConfig } from "./app-config";
import { EthPriceManager } from 'managers/eth-price-manager';
import { IdentityManager } from 'managers/identity-manager';
import { ToastsHandler } from 'services/os-toasts-handler';
import { PromotedTokensManager } from 'managers/promoted-tokens-manager';
import { TxnNotificationsManager } from 'managers/txn-notifications-manager';
import { Chain } from 'repeat';
import 'tippy.js/dist/tippy.css';
import * as signalR from '@microsoft/signalr';
import environment from '../config/environment.json';

@inject(DialogService, EventAggregator, EthPriceManager, IdentityManager, ToastsHandler, PromotedTokensManager, TxnNotificationsManager)
export class App {

  message = 'Hello Technoooooo!!';
  marketData = [];

  diclaimerVersion = '1.0.1';

  constructor(dialogService, eventAggregator, ethPriceManager, identityManager, toastsHandler, promotedTokensManager, txnNotificationsManager) {
    this._dialogService = dialogService;
    this._eventAggregator = eventAggregator;
    this._ethPriceManager = ethPriceManager;
    this._identityManager = identityManager;
    this._toastsHandler = toastsHandler;
    this._promotedTokensManager = promotedTokensManager;
    this._txnNotificationsManager = txnNotificationsManager;

    this.chain = new Chain();

    this.rtUrl = environment.rtUrl;

    this.connection = new signalR.HubConnectionBuilder().withUrl(this.rtUrl).build();

    this.connection.on('ReceiveMessage', (user, message) => {
      console.info('ReceiveMessage', user, message);
    });

    this.connection.on('Pong', () => {
      console.info('Pong');
    });

    this.txnNotifications = [];
    this.connection.on('NotifyTxn', async (txnNotification) => {
      // console.info('txnNotification', txnNotification);
      let header = null;
      let message = `<p>token: <a href="https://etherscan.io/token/${txnNotification.tokenAddress}" target="_blank"><b display:inline-block; width:240px; text-overflow: ellipsis; overflow: hidden; white-space: nowrap;>${txnNotification.tokenDisplayName.substring(0, 24)}</b></a></p>`;
      message += `<p>txn: <a href="https://etherscan.io/tx/${txnNotification.txnHash}" target="_blank">0x${txnNotification.txnHash.substring(2, 4)}..${txnNotification.txnHash.slice(-3)}</a></p>`;
      message += `<p>maker: <a href="https://etherscan.io/address/${txnNotification.makerAddress}" target="_blank">0x${txnNotification.makerAddress.substring(2, 4)}..${txnNotification.makerAddress.slice(-3)}</a></p>`;
      if (txnNotification.type == 'TokenCreation') {
        header = '<h6>TOKEN CREATION</h6>';
        this._toastsHandler.info(header + message);
      }
      else if (txnNotification.type == 'RenouncedOwnership') {
        header = '<h6>RENOUNCED OWNERSHIP</h6>';
        this._toastsHandler.success(header + message);
      }
      else if (txnNotification.type == 'RemovedLiquidity') {
        header = '<h6>REMOVED LIQUIDITY</h6>';
        this._toastsHandler.warning(header + message);
      }
      else if (txnNotification.type == 'SetFees') {
        header = '<h6>SET FEES</h6>';
        this._toastsHandler.info(header + message);
      }

      this.txnNotifications = await this._txnNotificationsManager.getTxnNotifications();
      this._eventAggregator.publish('txn-notifications-changed', this.txnNotifications);

    });

    this.connection.on('GameData', gameData => {
      this._eventAggregator.publish('game-data-changed', gameData);   
    });

    this.connection.start().then(() => {
      console.info('connected to rt');

      // this.connection.onclose(() => {
      //   setTimeout(() => {
      //     this.connection.start();
      //   }, 5000);
      // });

      this.connection.invoke('ping');
    }).catch(err => {
      return console.error(err);
    });

    setInterval(async () => {
      this.txnNotifications = await this._txnNotificationsManager.getTxnNotifications();
      this._eventAggregator.publish('txn-notifications-changed', this.txnNotifications);
    }, 60000);

    setInterval(() => {
      if(this.connection.state != 'Connected'){
        console.warn('rt is down, reconnecting..');
        this.connection.start();
      }
    }, 12000);

  }

  async activate() {
    await this._promotedTokensManager.getPromotedTokens();
  }

  configureRouter(config, router) {
    config.title = 'Market Visualizer';
    config.map(AppConfig.routes);
    config.fallbackRoute('');

    this.router = router;
    setTimeout(() => {
      this.navRoutes = router.routes.filter(_ => _.nav);
    });
  }

  async attached() {

    this.userInfo = this._identityManager.userInfo;
    this._eventAggregator.subscribe('userinfo-changed', () => {
      this.userInfo = this._identityManager.userInfo;
    });

    this.userInfo = await this._identityManager.getUserInfo();

    this.chain
      .add(() => this.getEthPrice())
      .every(60000);

    this.BADGE_ID = 'jg5OTA4NDUzMzkwN';

    this.ALCHEMY_URL = `https://alchemyapi.io/?r=badge:${this.BADGE_ID}`;
    this.ALCHEMY_ANALYTICS_URL = `https://analytics.alchemyapi.io/analytics`;

    let intervalId = setInterval(() => {
      const badge = document.getElementById('badge-button');
      if (badge && this.isBadgeInViewpoint(badge.getBoundingClientRect())) {
        this.logBadgeView();
        clearInterval(intervalId);
      }
    }, 2000);

    const disclaimerOpened = this.diclaimer();
    // if (!disclaimerOpened && !this.userInfo) this.openConnect();

    this.txnNotifications = await this._txnNotificationsManager.getTxnNotifications();
  }

  getEthPrice() {
    return this._ethPriceManager.get()
      .catch(error => {
        // console.error('getEthPrice', error);
      });
  }

  logBadgeClick() {
    fetch(`${this.ALCHEMY_ANALYTICS_URL}/badge-click`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        badge_id: this.BADGE_ID,
      }),
    });
    window.open(this.ALCHEMY_URL, '_blank').focus();
  }

  logBadgeView() {
    fetch(`${this.ALCHEMY_ANALYTICS_URL}/badge-view`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        badge_id: this.BADGE_ID,
      }),
    });
  }

  isBadgeInViewpoint(bounding) {
    return (
      bounding.top >= 0
      && bounding.left >= 0
      && bounding.bottom <= (window.innerHeight || document.documentElement.clientHeight)
      && bounding.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
  }

  diclaimer() {

    const item = JSON.parse(localStorage.getItem(`diclaimer-${this.diclaimerVersion}`));

    if (!item) {
      this.openDiclaimer();
      return true;
    }

    if (Date.now() >= item.expires) {
      localStorage.removeItem(`diclaimer-${this.diclaimerVersion}`);
      this.openDiclaimer();
      return true;
    }

    const value = item.value;
    if (!value) {
      this.openDiclaimer();
      return true;
    }

    return false;
  }

  openDiclaimer() {
    this._dialogService.open({
      viewModel: PLATFORM.moduleName('resources/dialogs/disclaimer'), model: null, lock: true
    }).whenClosed(response => {
      if (response.wasCancelled) return;
      // 2 days expiration
      const expirationTime = Date.now() + (2 * 24 * 60 * 60 * 1000);
      const item = {
        value: true,
        expires: expirationTime
      };
      localStorage.setItem(`diclaimer-${this.diclaimerVersion}`, JSON.stringify(item));

      // if (!this.userInfo) this.openConnect();
    });
  }

  openConnect() {
    return new Promise(async (resolve, reject) => {
      if (this.userInfo) {
        console.warn('already connected');
        this._toastsHandler.success('you are now connected to MarketViz and can start enjoying our full range of trading features.');
        resolve(true);
      }

      if (window.ethereum) {
        const accounts = await window.ethereum.request({ method: 'eth_requestAccounts' });
        const address = accounts[0];
        await this._identityManager.login(address);
        if (this.userInfo) this._toastsHandler.success('you are now connected to MarketViz and can start enjoying our full range of trading features.');
        resolve(!!this.userInfo);
      } else {
        console.error('Metamask not available');
        alert('To use MarketViz, you\'ll need to authenticate with Metamask by installing it and choosing a wallet. If you\'re using an iPhone, please note that you can still install Metamask but will need to use its built-in browser instead of Safari.');
        resolve(false);
      }
      return;

      this._dialogService.open({
        viewModel: PLATFORM.moduleName('resources/dialogs/connect'), model: null, lock: true
      }).whenClosed(async response => {
        if (response.wasCancelled) {
          resolve(false);
          return;
        }

        if (window.ethereum) {
          const accounts = await window.ethereum.request({ method: 'eth_requestAccounts' });
          const address = accounts[0];
          await this._identityManager.login(address);
          if (this.userInfo) this._toastsHandler.success('you are now connected to MarketViz and can start enjoying our full range of trading features.');
          resolve(!!this.userInfo);
        } else {
          console.error('Metamask not available');
          alert('To use MarketViz, you\'ll need to authenticate with Metamask by installing it and choosing a wallet. If you\'re using an iPhone, please note that you can still install Metamask but will need to use its built-in browser instead of Safari.');
          resolve(false);
        }
      });
    });
  }

  disconnect() {
    const test = confirm('you are about to disconnect your wallet from MARKETVIZ, are you sure about that??');
    if (test) {
      this._identityManager.logout();
    }
  }

  openNotifications() {
    // console.info('this.txnNotifications', this.txnNotifications);
    this._dialogService.open({
      viewModel: PLATFORM.moduleName('resources/dialogs/notifications'), model: this.txnNotifications, lock: false
    }).whenClosed(response => {
      if (response.wasCancelled) return;
    });
  }

}
