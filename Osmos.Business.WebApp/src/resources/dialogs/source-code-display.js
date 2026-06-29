import { inject } from 'aurelia-framework';
import { DialogController } from 'aurelia-dialog';

@inject(DialogController)
export class SourceCodeDisplay {

  constructor(controller) {
    this.controller = controller;
  }

  activate(model) {

    this.name = model.name;

    this.sourceCode = null;
    if (model.sourceCodeObject && !model.sourceCodeObject.missing && model.sourceCodeObject.result && model.sourceCodeObject.result.length && model.sourceCodeObject.result[0]) {
      let str = model.sourceCodeObject.result[0].sourceCode;
      str = str.substring(1);
      str = str.slice(0, -1);
      let result = null;
      try {
        result = JSON.parse(str);
      } catch (error) {
        this.sourceCode = str;
      }

      if (result && result.sources) {
        let sources = null;
        for (let prop in result.sources) {
          sources = result.sources[prop];
          break;
        }
        this.sourceCode = sources.content;
      }
    }
  }

  ok() {
    this.controller.ok({});
  }
}
