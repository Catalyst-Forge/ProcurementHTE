# Procurement HTE Production Deployment

Production source branch: `master`.

The VPS deploys from `master` with `/srv/procurementhte/deploy.sh`. A systemd timer checks for changes and deploys only when the remote SHA changes.

## DNS

Create this DNS record before enabling HTTPS:

```text
Type: A
Name: hte
Value: 43.157.203.169
TTL: 300
```

## Server Paths

```text
/srv/procurementhte/repo      Git checkout
/srv/procurementhte/releases  Published releases
/srv/procurementhte/current   Active release symlink
/etc/procurementhte           Production environment file
/var/lib/procurementhte/keys  DataProtection keys
```

## Useful Commands

```bash
sudo systemctl status procurementhte-web --no-pager
sudo journalctl -u procurementhte-web -f
sudo systemctl start procurementhte-deploy.service
sudo systemctl list-timers | grep procurementhte
```

After DNS propagates:

```bash
sudo certbot --nginx -d hte.catalystforge.web.id --redirect
```
