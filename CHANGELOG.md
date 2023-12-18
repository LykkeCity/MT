## 2.29.0 - Nova 2. Delivery 38 (December 13, 2023)
### What's changed
* LT-5062: Rfqevent rabbit mq re-configuration.
* LT-5034: Open orders api - sort chronologically.
* LT-5010: Change the logic for trading core snapshots.

### Deployment
* In `RfqChanged` configuration section (`/MtBackend/RabbitMqPublishers/RfqChanged`) just add field with connection string. If there is already one, ensure connection string is pointing to **multi-broker Rabbit MQ** instance. Also ensure that main Rabbit MQ configuration (`/MtBackend/MtRabbitMqConnString`) is pointing to **broker-level instance of Rabbit MQ**.
* Existing feature `Create trading snapshot` has been extended to include possibility to create snapshot in `Draft` status. Here no actions required.

## 2.28.1 - Nova 2. Delivery 37. Hotfix 2 (2023-11-24)
### What's Changed
* fix(LT-5079): Rollback major RabbitMQ updates

**Full Changelog**: https://github.com/LykkeBusiness/MT/compare/v2.28.0...v2.28.1
