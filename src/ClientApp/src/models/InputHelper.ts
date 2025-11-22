export function debounce(fn: (...args: unknown[]) => unknown, ms = 300) {
  let debounceTimeoutId: ReturnType<typeof setTimeout>;

  return function (this: unknown, ...args: unknown[]) {
    clearTimeout(debounceTimeoutId);
    debounceTimeoutId = setTimeout(() => fn.apply(this, args), ms);
  };
}

export function composeFix(event: Event) {
  // Fix for IME input on some Android keyboards
  if (event.target instanceof HTMLInputElement) {
    (event.target as any).composing = false;
  }
}
