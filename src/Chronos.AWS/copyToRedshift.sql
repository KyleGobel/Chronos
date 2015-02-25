begin transaction;

create temp table $TABLENAME$_staging (like $TABLENAME$);

copy $TABLENAME$_staging ($COLUMNLIST$)
from 's3://mobi-email/$PATH$'
credentials 'aws_access_key_id=$ACCESSKEY$;aws_secret_access_key=$SECRETKEY$'
delimiter '\t'
ignoreblanklines BLANKSASNULL ACCEPTANYDATE TRUNCATECOLUMNS GZIP timeformat 'auto';

delete from $TABLENAME$
using $TABLENAME$_staging
where
$PRIMARYKEYCHECK$;

 insert into $TABLENAME$ 
 select * from $TABLENAME$_staging;

 drop table $TABLENAME$_staging;

 end transaction;