copy $TABLENAME$ ($COLUMNLIST$)
from 's3://$BUCKET$/$PATH$'
credentials 'aws_access_key_id=$ACCESSKEY$;aws_secret_access_key=$SECRETKEY$'
delimiter '$DELIMETER$'
ignoreheader as $HEADERROWS$
ignoreblanklines BLANKSASNULL ACCEPTANYDATE TRUNCATECOLUMNS GZIP timeformat 'auto';