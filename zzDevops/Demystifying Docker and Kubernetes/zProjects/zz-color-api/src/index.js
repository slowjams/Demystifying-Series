const express = require('express');
const os = require('os');

const app = express();
const port = 80;
const color = "blue";
const hostname = os.hostname();

const delay_startup = process.env.DELAY_STARTUP === 'true';
const fail_liveness = process.env.FAIL_LIVENESS === 'true';
const fail_readiness = process.env.FAIL_READINESS === 'true' ? Math.random() < 0.5 : false;

console.log(`Delay startup: ${delay_startup}`);
console.log(`Fail liveness: ${fail_liveness}`);
console.log(`Fail readiness: ${fail_readiness}`);

app.get('/', (req, res) => {
  res.send(`<h1 style="color:${color};">Hello from color-api!</h1>
    <h2>Hostname: ${hostname}</h2>`);
});

app.get('/api', (req, res) => {
  const { format } = req.query;  // localhost/api?format=text

  if (format === 'json') {
    return res.json({
      color,
      hostname
    });
  } else {
    return res.send(`Color: ${color}, Hostname: ${hostname}`);
  }
});

//--------------------------------V 
app.get('/ready', (req, res) => {
  if (fail_readiness) {
    return res.sendStatus(503);
  }

  return res.send('ok');
});

app.get('/up', (req, res) => {
  return res.send('ok');
});

app.get('/health', (req, res) => {
   if (fail_liveness) {
    return res.sendStatus(503);
  }

  return res.send('ok');
});

if (delay_startup) {
  const start = Date.now();
  while (Date.now() - start < 60000) {
    // Simulate a delay of 60 seconds
  }
}
//--------------------------------Ʌ

app.listen(port, () => {
  console.log(`color-api listening on ${port}`);
});