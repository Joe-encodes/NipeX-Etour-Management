module.exports = {
  transformIgnorePatterns: [
    "/node_modules/(?!(axios)/)"
  ],
  testEnvironment: "jsdom",
  moduleNameMapper: {
    "\\.(css|less|scss|sass)$": "identity-obj-proxy"
  }
};
