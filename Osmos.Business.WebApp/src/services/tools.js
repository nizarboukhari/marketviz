export function copyArray(oldArray, newArray, condition) {

  console.info('newArray.length', newArray.length);

  const toDelete = [];
  for (let index = 0; index < oldArray.length; index++) {
    const _n = newArray.find(condition(oldArray[index]));
    if (_n) {
      oldArray[index].val = _n.val;
      continue;
    }

    toDelete.push(oldArray[index]);
  }

  oldArray = oldArray.filter(_0 => !toDelete.find(condition(_0)));

  for (let index = 0; index < newArray.length; index++) {
    const _o = oldArray.find(condition(newArray[index]));
    if (_o) continue;

    oldArray.push(newArray[index]);
  }

  return oldArray;
}
