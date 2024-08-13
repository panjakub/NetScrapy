# NetScrapy
Small and opinionated tool for scraping pages, with Playwright included. 

# Usage
Create a file following the json schema config. Build the solution (install Playwright's dependencies if needed), and run with config.json file as an argument. 

# Storage
For now you have to have SQL Server installed locally with the ScrapyDB database. Run the migrations and you're good to go. 

# Limitations
Politeness is built-in, at least partially. For now you should set crawl delay yourself (I recommend at least 2000ms), but test what works best for you. 

Robots.txt can be parsed if provided in the config, if you do this, NetScrapy will take care of respecting it if you provide the correct UserAgent. 

# Parallelism
By default, the scraping happen in batches. Batch size is number of unique domains in your config, so technically more sites may have better average scores than if you run just a few domains, as the batches will be bigger and cover more items at the same time. 

# Tests
Yeah I'm working on those (he said not working on those). Initially it was supposed to be a quick hacky project that suddenly grew and became useful so I decided to share. 

# Pull requests, bug reports, ideas
I mean, if you want to. Not looking for contributors at the moment, but if you have some ideas feel free to implement, I'll be happy to review and add them. 