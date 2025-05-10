# Zooscape Dotnet starter Bot

## Installation

To run the Dotnet bot, make sure you have the dotnet 8 runtime

## Running

### IDE
To run the Dotnet bot, you can either use **Visual Studio** or any IDE that is able to run dotnet.

### Docker
Build the docker image by running `docker build -t <image_name> .` in the root directory i.e. /NETCoreBot  
Then run the container using `docker run --env=RUNNER_IPV4=host.docker.internal netcorebot`. Be sure to have the engine running before you run your bot.  
You can change the container name by adding the [`--name`](https://docs.docker.com/engine/reference/commandline/run/#name) option to the run command.  
You can also change the name of the NETCoreBot in the logs by adding the `--env=BOT_NICKNAME=MyBotName` option to the run command  