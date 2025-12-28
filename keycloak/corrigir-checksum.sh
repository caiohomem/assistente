#!/bin/bash
# Script para corrigir o checksum do Liquibase no banco Neon

PGPASSWORD='npg_dcn6oJIReT0Z' psql -h ep-spring-star-abbjctg7-pooler.eu-west-2.aws.neon.tech -U neondb_owner -d neondb <<EOF
-- Corrige o checksum problemático
UPDATE databasechangelog 
SET md5sum = '9:3a32bace77c84d7678d035a7f5a8084e' 
WHERE id = '2.5.0-unicode-oracle' 
  AND author = 'hmlnarik@redhat.com';

-- Verifica se foi atualizado
SELECT id, author, md5sum 
FROM databasechangelog 
WHERE id = '2.5.0-unicode-oracle' 
  AND author = 'hmlnarik@redhat.com';
EOF

echo "Checksum corrigido!"

