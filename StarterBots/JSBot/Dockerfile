# Base javascript image
FROM public.ecr.aws/m5z5a5b2/languages/javascript:2021

# Change working directory to execute the following commands
WORKDIR /app

# Copy code into this image's current working directory
COPY ./ .

# Install dependencies
RUN npm i

# The entrypoint to start the bot
ENTRYPOINT ["npm", "start"]