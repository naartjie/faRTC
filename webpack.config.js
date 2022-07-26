module.exports = {
  mode: "development",
  devtool: "source-map",
  // devtool: "eval-source-map",
  entry: "./src/App.fs.js",
  output: {
    path: "./public",
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
