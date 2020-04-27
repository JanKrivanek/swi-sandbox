#--build -V 
#   to invoke rebuild
#--scale cache_consumer=3 
#   to run 3 instances of cache consumer service
docker-compose up --build -V --scale cache_consumer=3