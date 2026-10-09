const express = require('express');

const app = express();
const port = 80;

app.get('/', (req, res) => {
  res.send('<h1 style="color:blue;">Hello from color-api!</h1>');
});

app.listen(port, () => {
  console.log(`color-api listening on ${port}`);
});