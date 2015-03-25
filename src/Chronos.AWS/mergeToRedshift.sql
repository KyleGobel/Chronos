copy $TABLENAME$ ($COLUMNLIST$)
from 's3://mobi-email/$PATH$'
credentials 'aws_access_key_id=$ACCESSKEY$;aws_secret_access_key=$SECRETKEY$'
delimiter '\t'
ignoreheader as $HEADERROWS$
ignoreblanklines BLANKSASNULL ACCEPTANYDATE TRUNCATECOLUMNS GZIP timeformat 'auto';
