const keyValueDb = process.env.KEY_VALUE_DB;
const keyValueUser = process.env.KEY_VALUE_USER;
const keyValuePassword = process.env.KEY_VALUE_PASSWORD;

console.log('Initializing : Key-Value DB User');

db = db.getSiblingDB(keyValueDb);

db.createUser({
  user: keyValueUser,
  pwd: keyValuePassword,
  roles: [
    {
      role: 'readWrite',
      db: keyValueDb,
    },
  ],
});

/*
Docker
  │
  │ starts ENTRYPOINT
  ▼
docker-entrypoint.sh
  │
  │ invokes
  ▼
mongosh (has js runtime)
  │
  │ executes
  ▼
mongo-init.js
  │
  │ sends database commands
  ▼
mongod  (MongoDB server process — the actual program that runs the database.)
*/
