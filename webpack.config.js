var path = require("path");

module.exports = {
  mode: "development",
  // devtool: "eval-source-map",
  devtool: "source-map",
  entry: "./src/App.fs.js",
  output: {
    path: path.join(__dirname, "./public"),
    filename: "bundle.js",
  },
  devServer: {
    devMiddleware: {
      publicPath: "/",
    },
    static: {
      directory: "./public",
    },
    port: 8001,
  },
  module: {
    rules: [
      {
        test: /\.js$/,
        enforce: "pre",
        use: ["source-map-loader"],
      },
    ],
  },
};
