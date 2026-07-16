#!/usr/bin/env bash
#
# TelcoLab end-to-end demo.
#
# Shows a subscription grain coordinating an asynchronous number port with an
# external (simulated) clearing house:
#   Active -> Porting -> [webhook arrives] -> Active | PortingRejected
#
# Usage:
#   Terminal 1:  cd src/TelcoLab.ClearingHouse && ASPNETCORE_URLS=http://localhost:5080 dotnet run --no-launch-profile
#   Terminal 2:  cd src/TelcoLab.Silo         && ASPNETCORE_URLS=http://localhost:5100 dotnet run --no-launch-profile
#   Terminal 3:  ./demo.sh
#
set -euo pipefail

SILO="http://localhost:5100"

# status codes: 0 Inactive, 1 Active, 2 Porting, 3 PortingRejected, 4 PortingCancelled
show() { echo "   state: $(curl -s "$SILO/subscriptions/$1")"; }

echo "=== CASE A — number that ports successfully (+34600000011) ==="
curl -s -X POST "$SILO/subscriptions/+34600000011/activate" >/dev/null
echo "1) activated"; show "+34600000011"
curl -s -X POST "$SILO/subscriptions/+34600000011/port" \
     -H "Content-Type: application/json" -d '{"donorOperator":"ACME"}' >/dev/null
echo "2) port requested -> now Porting, waiting on the clearing house"; show "+34600000011"

echo
echo "=== CASE B — number the clearing house rejects (+34600000099) ==="
curl -s -X POST "$SILO/subscriptions/+34600000099/activate" >/dev/null
curl -s -X POST "$SILO/subscriptions/+34600000099/port" \
     -H "Content-Type: application/json" -d '{"donorOperator":"ACME"}' >/dev/null
echo "1) activated + port requested -> Porting"; show "+34600000099"

echo
echo "... webhooks are asynchronous; waiting 5s for the clearing house to reply ..."
sleep 5

echo
echo "=== FINAL STATE (resolved by inbound webhooks) ==="
echo "A (+34600000011) -> expected Active (1):"; show "+34600000011"
echo "B (+34600000099) -> expected PortingRejected (3), reason NumberNotPortable (2):"; show "+34600000099"
