window.requestVerificationHeaders = function (headers) {
  const token = document.querySelector('meta[name="request-verification-token"]')?.content;
  return token ? { ...headers, RequestVerificationToken: token } : headers;
};
