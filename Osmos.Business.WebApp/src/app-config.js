import { PLATFORM } from 'aurelia-pal';

export class AppConfig {
  static routes = [
    {
      route: [''],
      moduleId: PLATFORM.moduleName('vcs/tokens/trending'),
      title: 'hot-tokens',
      nav: {
        header: '<span class="title main">Hot Ethereum Tokens in <span class="active-link"> Real Time</span></span>',
      }
    },
    {
      route: ['best-performance'],
      moduleId: PLATFORM.moduleName('vcs/tokens/best-of'),
      title: 'best performance',
      nav: {
        header: '<span class="title main">Best Performance in <span class="active-link"> The Last 24 Hours</span></span>',
      }
    },
    {
      route: ['play'],
      moduleId: PLATFORM.moduleName('vcs/play/play'),
      title: 'play',
      nav: {
        header: '<span class="title main">Market<span class="active-link"> Play (BETA)</span></span>',
      }
    },
    // {
    //   route: ['releases'],
    //   moduleId: PLATFORM.moduleName('vcs/releases/releases'),
    //   title: 'releases',
    //   nav: {
    //     header: '<span class="title main">What happened in the matter of <span class="active-link"> Versioning</span></span>',
    //   }
    // },
    {
      route: 'txns',
      moduleId: PLATFORM.moduleName('vcs/txns/txns'),
      title: 'txns',
    },
    {
      route: 'txns/token-txns',
      moduleId: PLATFORM.moduleName('vcs/token-txns/token-txns'),
      title: 'token-txns',
    }
  ];
}
