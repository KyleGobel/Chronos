begin transaction;

create temp table $TEMPTABLE$ (like $TABLENAME$);

copy $TEMPTABLE$ ($COLUMNLIST$)
from 's3://$BUCKET$/$PATH$'
credentials 'aws_access_key_id=$ACCESSKEY$;aws_secret_access_key=$SECRETKEY$'
delimiter '$DELIMETER$'
ignoreheader as $HEADERROWS$
ignoreblanklines BLANKSASNULL ACCEPTANYDATE TRUNCATECOLUMNS GZIP timeformat 'auto';

delete from $TABLENAME$
using $TEMPTABLE$ 
where
$PRIMARYKEYCHECK$;

 insert into $TABLENAME$ 
 select * from $TEMPTABLE$;

 drop table $TEMPTABLE$;

 end transaction;