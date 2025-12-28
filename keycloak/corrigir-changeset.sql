-- Atualiza o checksum para o valor correto esperado pelo Keycloak
UPDATE databasechangelog 
SET md5sum = '8:dc22559d51a684e2a0eacb0d5c9a4740'
WHERE id = '25.0.0-28265-index-cleanup' 
  AND author = 'keycloak';

-- Se o changeset não existir, marca como executado com o checksum correto
INSERT INTO databasechangelog (
    id, 
    author, 
    filename, 
    dateexecuted, 
    orderexecuted, 
    exectype, 
    md5sum, 
    description, 
    comments, 
    tag, 
    liquibase, 
    contexts, 
    labels, 
    deployment_id
) 
SELECT 
    '25.0.0-28265-index-cleanup',
    'keycloak',
    'META-INF/jpa-changelog-25.0.0.xml',
    NOW(),
    COALESCE((SELECT MAX(orderexecuted) FROM databasechangelog), 0) + 1,
    'EXECUTED',
    '8:dc22559d51a684e2a0eacb0d5c9a4740',
    'index-cleanup',
    '',
    NULL,
    '4.27.0',
    NULL,
    NULL,
    'manual-fix'
WHERE NOT EXISTS (
    SELECT 1 FROM databasechangelog 
    WHERE id = '25.0.0-28265-index-cleanup' 
      AND author = 'keycloak'
);

-- Verifica o resultado
SELECT id, author, md5sum, exectype 
FROM databasechangelog 
WHERE id = '25.0.0-28265-index-cleanup' 
  AND author = 'keycloak';

