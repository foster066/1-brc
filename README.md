# 1 Billion Row Challenge

1 billion rows challenge implementation. Original challenge can be found here: https://1brc.dev/
## My modifications

 - The program will write output to the file instead of printing it to the console. That way it will bea easier to verify for correctness
 
##  Implementation concepts
 - The program will be implemented as a runner can execute different implementations specified by an implementation name
 - The program will print out total run time of an implementation
 - The program will support the following parameters:
	 1. Input data file path
	 2. Implementation name
 ## Implementations
 Here I list all implementations, they pros and cons and details
 

| Name | Stages | ✅Pros | ❌Cons |⏳Result|
|--|--|--|--|--|
|**Implentation1** <br/> The first implementation to try out. Uses simple but effective ways of file processing. <br/>One thread, input file is read and split by files for each station |1. Read input file by 16 kB chunks. Then write a station measurement to the separate file<br/> 2. Read measurements of a station and calculate Avg, Max and Min one by one<br/> 3. Write the results file sorting by station name|1. Relatively simple implementation<br/>2. Minimal allocations<br>3. Can handle very large input data|1. Low input file read speed<br>2. Need for a calculation stage (stage 2)<br>3. One thread so it's long|Stage 1 - 00:03:20.26781<br>Stage 2 - 00:01:29.93<br>**Total - 00:04:50.41**|
|**Implentation2** <br/> One thread implementation. <br/>Removed all extra steps, results are calculating while reading data file. Max profiling and optimizations of all operations.|1. Reading the input file and calculating values - min, max and sum of values. <br>2. Write results to file calculating average on place. <br/> Most of the code is benchmarked.|1. Using Memory Mapped File<br/>2. No allocations <br/>3. Using AlternativeLookup for Dictionaries<br/>4. No parsing of doubles. Using integers instead. <br/>5. Custom integer parsing.<br/>6. Custom IndexOf implementation.|1. Using binary reader because of Memory Mapped File complicates working with buffers.<br/>2. Implementation is still slow. |**Total - 02:17.42**|

