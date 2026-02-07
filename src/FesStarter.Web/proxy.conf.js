const target = process.env['services__api__http__0'] || 'http://localhost:5000';

module.exports = {
  '/api': {
    target,
    secure: false,
    changeOrigin: true
  }
};
