import environment from '../config/environment.json';
import {PLATFORM} from 'aurelia-pal';
import '@fortawesome/fontawesome-free/css/all.css';
import 'noty/lib/noty.css';
import 'noty/lib/themes/nest.css';
import 'noty/lib/themes/bootstrap-v3.css';
import 'noty/lib/themes/bootstrap-v4.css';
import 'noty/lib/themes/light.css';
import 'noty/lib/themes/metroui.css';
import 'noty/lib/themes/mint.css';
import 'noty/lib/themes/relax.css';
import 'noty/lib/themes/semanticui.css';
import 'noty/lib/themes/sunset.css';

export function configure(aurelia) {
  aurelia.use
    .standardConfiguration()
    .plugin(PLATFORM.moduleName('aurelia-dialog'), config => {
      config.useDefaults();
      // config.useCSS('');
    })
    .feature(PLATFORM.moduleName('resources/index'));

  aurelia.use.developmentLogging(environment.debug ? 'debug' : 'warn');

  if (environment.testing) {
    aurelia.use.plugin(PLATFORM.moduleName('aurelia-testing'));
  }

  aurelia.start().then(() => aurelia.setRoot(PLATFORM.moduleName('app')));
}
