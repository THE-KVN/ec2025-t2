# Zooscape JavaScript Starter Bot

## Installation

To get started, install dependencies by running the following command

```sh
npm i
```

## Developing

Edit the code in `src/index.js` to make your bot go! 

## Running the bot

To run the bot simply run

```sh
npm start
```

### Docker
Build the docker image by running `docker build -t <image_name> .` in the root directory i.e. /JSBot  
Then run the container using `docker run --env=RUNNER_IPV4=host.docker.internal jsbot`. Be sure to have the engine running before you run your bot.  
You can change the container name by adding the [`--name`](https://docs.docker.com/engine/reference/commandline/run/#name) option to the run command.  
You can also change the name of the JSBot in the logs by adding the `--env=BOT_NICKNAME=MyBotName` option to the run command  