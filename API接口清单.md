# SRM 后端接口清单

**通用说明**

- 所有 Controller 路由前缀统一为 `[controller]/[action]`，只有 `LoginController` 保留 `api/Login/[action]`
- 绝大多数接口采用 `ApiResult` 通用包装响应：

```json
{
  "success": true,
  "message": "操作成功",
  "data": { ... }
}
```

- `?` 后缀表示可选字段

---

## 一、登录认证

### POST `/login`

**说明**：用户登录，验证账号密码，返回 JWT Token

**请求体**：
```json
{
  "userCode": "admin",
  "password": "123456"
}
```

**成功响应（200）**：
```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "user": {
      "userID": "guid",
      "userCode": "admin",
      "userName": "管理员",
      "roles": ["采购员", "管理员"]
    }
  }
}
```

**失败响应**：`400`（参数为空）/ `401`（账号或密码错误）

---

## 二、用户管理 — `UserController`

> 路由前缀：`[controller]/[action]` → `User/xxx`

### POST `User/AddUser`

**说明**：添加用户，账号唯一

**请求体**：
```json
{
  "userCode": "zhangsan",
  "userName": "张三",
  "password": "123456",
  "memo?": "备注"
}
```

**成功响应（200）**：
```json
{
  "success": true,
  "message": "用户添加成功",
  "data": {
    "userID": "guid",
    "userCode": "zhangsan",
    "userName": "张三",
    "isDel": false,
    "memo": "备注",
    "createTime": "2026-06-23T10:00:00",
    "updateTime": null
  }
}
```

**失败**：`400` 参数为空 / `409` 账号已存在

---

### POST `User/DeleteUser`

**说明**：删除用户（软删除，设 `IsDel = true`）

**请求参数**（query）：

| 参数 | 类型 | 说明 |
|------|------|------|
| id | string | 用户ID |

**成功响应（200）**：
```json
{ "success": true, "message": "用户已删除（软删除）" }
```

**失败**：`404` 用户不存在

---

### PUT `User/UpdateUser`

**说明**：修改用户信息，部分字段可选

**请求体**：
```json
{
  "userID": "guid",
  "userCode": "zhangsan_new",
  "userName": "张三",
  "password?": "newpwd",
  "memo?": "新备注"
}
```

**成功响应（200）**：
```json
{
  "success": true,
  "message": "用户修改成功",
  "data": {
    "userID": "guid",
    "userCode": "zhangsan_new",
    "userName": "张三",
    "isDel": false,
    "memo": "新备注",
    "createTime": "2026-06-23T10:00:00",
    "updateTime": "2026-06-24T10:00:00"
  }
}
```

**失败**：`404` 用户不存在

---

### GET `User/GetUsers`

**说明**：查询所有用户（含已禁用的）

**成功响应（200）**：
```json
{
  "success": true,
  "message": "查询成功",
  "data": [
    {
      "userID": "guid",
      "userCode": "zhangsan",
      "userName": "张三",
      "isDel": false,
      "memo": null,
      "createTime": "2026-06-23T10:00:00",
      "updateTime": null,
      "roles": []
    }
  ]
}
```

---

### PUT `User/ToggleUserStatus`

**说明**：切换用户启用/禁用状态（`IsDel` 取反）

**请求体**：
```json
{ "id": "guid" }
```

**成功响应（200）**：
```json
{ "success": true, "message": "已禁用" }
// 或
{ "success": true, "message": "已启用" }
```

**失败**：`404` 用户不存在

---

### POST `User/AddRole`

**说明**：添加角色，角色名唯一

**请求体**：
```json
{
  "roleName": "采购员",
  "memo?": "角色备注"
}
```

**成功响应（200）**：
```json
{
  "success": true,
  "message": "角色添加成功",
  "data": {
    "roleID": "guid",
    "roleName": "采购员",
    "isDel": false,
    "memo": "角色备注",
    "createTime": "2026-06-23T10:00:00",
    "updateTime": null
  }
}
```

**失败**：`400` 参数为空 / `409` 角色名已存在

---

### POST `User/DeleteRole`

**说明**：删除角色（**物理删除**），角色下存在用户时拒绝

**请求参数**（query）：

| 参数 | 类型 | 说明 |
|------|------|------|
| id | string | 角色ID |

**成功响应（200）**：
```json
{ "success": true, "message": "角色已删除" }
```

**失败**：
- `404` — 角色不存在
- `400` — `"该角色下存在 N 个用户，请先移除所有用户后再删除"`

---

### PUT `User/UpdateRoleStatus`

**说明**：启用/禁用角色

**请求体**：
```json
{
  "roleId": "guid",
  "isDel": true
}
```

**成功响应（200）**：
```json
{ "success": true, "message": "操作成功" }
```

**失败**：`404` 角色不存在

---

### GET `User/GetRoles`

**说明**：查询所有角色

**成功响应（200）**：
```json
{
  "success": true,
  "message": "查询成功",
  "data": [
    {
      "roleID": "guid",
      "roleName": "采购员",
      "isDel": false,
      "memo": null,
      "createTime": "2026-06-23T10:00:00",
      "updateTime": null,
      "userCount": 0
    }
  ]
}
```

---

### PUT `User/ToggleRoleStatus`

**说明**：切换角色启用/禁用状态（`IsDel` 取反）

**请求体**：
```json
{ "id": "guid" }
```

**成功响应（200）**：
```json
{ "success": true, "message": "已禁用" }
// 或
{ "success": true, "message": "已启用" }
```

**失败**：`404` 角色不存在

---

## 三、用户-角色关联 — `UserController`

### GET `User/GetUserRoles`

**说明**：查询所有用户-角色关联记录

**成功响应（200）**：
```json
{
  "success": true,
  "message": "查询成功",
  "data": [
    {
      "userRoleID": "guid",
      "userID": "guid",
      "roleID": "guid"
    }
  ]
}
```

---

### POST `User/AddUserToRole`

**说明**：将用户添加到角色

**请求体**：
```json
{
  "userID": "guid",
  "roleID": "guid"
}
```

**成功响应（200）**：
```json
{ "success": true, "message": "用户已添加到角色" }
```

**失败**：`404`（用户/角色不存在）/ `409`（已存在关联）

---

### DELETE `User/RemoveUserFromRole`

**说明**：将用户从角色移除（物理删除关联记录）

**请求体**（from body）：
```json
{
  "userID": "guid",
  "roleID": "guid"
}
```

**成功响应（200）**：
```json
{ "success": true, "message": "用户已从角色移除" }
```

**失败**：`404` 该用户不在指定角色中

---

## 四、供应商管理 — `SupplierController`

> 路由前缀：`[controller]/[action]` → `Supplier/xxx`

### POST `Supplier/GetAllSuppliers`

**说明**：查询所有供应商

**成功响应（200）**：
```json
{
  "success": true,
  "message": "查询成功",
  "data": [
    {
      "supplierID": "guid",
      "supplierCode": "GYS001",
      "supplierName": "XX供应商",
      "people": "联系人",
      "phoneNumber": "13800138000",
      "address?": "地址",
      "isDel": false,
      "memo?": "备注"
    }
  ]
}
```

---

### POST `Supplier/UpdateSupplier`

**说明**：修改供应商信息，已禁用的供应商不可修改

**请求体**：
```json
{
  "supplierID": "guid",
  "supplierCode": "GYS001",
  "supplierName": "XX供应商",
  "people": "联系人",
  "phoneNumber": "13800138000",
  "address?": "新地址",
  "memo?": "新备注"
}
```

**成功响应（200）**：
```json
{ "success": true, "message": "供应商信息修改成功" }
```

**失败**：`400`（禁用中）/ `404`（不存在）

---

### POST `Supplier/UpdateSupplierStatus`

**说明**：启用/停用供应商

**请求体**：
```json
{
  "supplierID": "guid",
  "isDel": true
}
```

**成功响应（200）**：
```json
{ "success": true, "message": "供应商状态已修改成功" }
```

**失败**：`404` 供应商不存在

---

## 五、物料管理 — `MaterialsController`

> 路由前缀：`[controller]/[action]` → `Materials/xxx`

### GET `Materials/GetAll`

**说明**：获取所有启用的物料列表（过滤 `IsDel == false`，按物料编码排序）

**成功响应（200）**：
```json
{
  "success": true,
  "message": "查询成功",
  "data": [
    {
      "materialID": "guid",
      "materialCode": "WL001",
      "materialName": "电阻",
      "spec": "100Ω",
      "unit": "个",
      "memo?": null
    }
  ]
}
```

---

## 六、采购订单 — `OrdersController`

> 路由前缀：`[controller]/[action]` → `Orders/xxx`

### POST `Orders/CreateOrder`

**说明**：创建采购订单，自动编号（格式 `POyyyyMMddXXX`），从 JWT 获取创建人

**请求体**：
```json
{
  "supplierID": "guid",
  "supplierName": "XX供应商",
  "memo?": "订单备注",
  "materials": [
    {
      "materialID": "guid",
      "qty": 100,
      "unitPrice?": 5.50
    }
  ]
}
```

**成功响应（200）**：
```json
{
  "success": true,
  "message": "采购订单创建成功",
  "data": {
    "orderID": "guid",
    "orderCode": "PO20260623ABC",
    "status": 0,
    "createTime": "2026-06-23T10:00:00"
  }
}
```

**失败**：`400` 参数为空 / `404` 供应商/物料不存在

---

### GET `Orders/GetOrdersByList`

**说明**：分页查询采购订单，支持模糊搜索和状态筛选

**请求参数**（query）：

| 参数 | 类型 | 必填 | 默认 | 说明 |
|------|------|------|------|------|
| orderCode | string | 否 | — | 订单编号（模糊匹配） |
| supplierID | string | 否 | — | 供应商ID（精确） |
| status | int | 否 | — | 0-待确认 1-已确认 2-待发货 3-已发货 4-已收货 |
| pageIndex | int | 否 | 1 | 页码 |
| pageSize | int | 否 | 20 | 每页条数 |

**成功响应（200）**：
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "total": 50,
    "pageIndex": 1,
    "pageSize": 20,
    "list": [
      {
        "orderID": "guid",
        "orderCode": "PO20260623ABC",
        "supplierID": "guid",
        "supplierName": "XX供应商",
        "supplierContact": "联系人",
        "supplierPhone": "13800138000",
        "status": 0,
        "statusName": "待确认",
        "createByID": "guid",
        "createByName": "张三",
        "createTime": "2026-06-23T10:00:00",
        "updateTime": "2026-06-23T10:00:00",
        "memo": null,
        "orderDetails": [
          {
            "orderDetailID": "guid",
            "materialCode": "WL001",
            "materialName": "电阻",
            "spec": "100Ω",
            "unit": "个",
            "qty": 100,
            "unitPrice": 5.50,
            "amount": 550
          }
        ]
      }
    ]
  }
}
```

---

### POST `Orders/ConfirmOrder`

**说明**：确认采购订单（状态 0 → 1），从 JWT 获取操作人

**请求体**：
```json
{ "id": "guid" }
```

**成功响应（200）**：
```json
{
  "success": true,
  "message": "订单确认成功",
  "data": {
    "orderID": "guid",
    "orderCode": "PO20260623ABC",
    "status": 1,
    "statusName": "已确认"
  }
}
```

**失败**：`400` 订单状态不允许确认（非待确认）

---

## 七、送货管理 — `DeliveryController`

> 路由前缀：`[controller]/[action]` → `Delivery/xxx`

### POST `Delivery/CreateDeliveryNote`

**说明**：根据采购订单生成送货单，自动编号（格式 `DSHyyyyMMdd001`）。`detailQuantities` 可选，不传则按采购数量全部送货。

**请求体**：
```json
{
  "orderID": "guid",
  "expectedDate?": "2026-07-01",
  "createByID": "guid",
  "createByName": "张三",
  "detailQuantities?": [
    {
      "materialCode": "WL001",
      "materialName?": "电阻",
      "unit?": "个",
      "quantity": 50
    }
  ]
}
```

**成功响应（200）**：
```json
{
  "code": 200,
  "message": "送货单创建成功",
  "data": {
    "noteID": "guid",
    "noteCode": "DSH20260623001",
    "orderID": "guid",
    "supplierID": "guid",
    "supplierName": "XX供应商",
    "status": false,
    "expectedDate": "2026-07-01T00:00:00",
    "createdTime": "2026-06-23T10:00:00",
    "detailCount": 3
  }
}
```

**失败**：`400`（参数为空/超量/状态不符）/ `404`（订单不存在）

---

### DELETE `Delivery/DeleteDeliveryNote`

**说明**：删除送货单（软删除），有收料记录时不可删除

**请求参数**（path）：

| 参数 | 类型 | 说明 |
|------|------|------|
| noteId | string | 送货单ID |

**成功响应（200）**：
```json
{ "code": 200, "message": "送货单已删除" }
```

**失败**：`400` 该送货单已有收料记录，无法删除 / `404` 不存在

---

### POST `Delivery/GetDeliveryNote`

**说明**：分页查询送货单列表（含明细）

**请求体**：
```json
{
  "noteCode?": "DSH20260623",
  "supplierId?": "guid",
  "status?": true,
  "page": 1,
  "pageSize": 10
}
```

**成功响应（200）**：
```json
{
  "code": 200,
  "data": {
    "items": [
      {
        "noteID": "guid",
        "noteCode": "DSH20260623001",
        "orderID": "guid",
        "supplierID": "guid",
        "supplierName": "XX供应商",
        "status": false,
        "expectedDate": "2026-07-01T00:00:00",
        "deliveryDate": null,
        "createByName": "张三",
        "createdTime": "2026-06-23T10:00:00",
        "details": [
          {
            "detailID": "guid",
            "materialCode": "WL001",
            "materialName": "电阻",
            "unit": "个",
            "quantity": 100,
            "receivedQty": 0
          }
        ]
      }
    ],
    "total": 20,
    "page": 1,
    "pageSize": 10
  }
}
```

---

## 八、收料管理 — `ReceiveController`

> 路由前缀：`[controller]/[action]` → `Receive/xxx`

### POST `Receive/CreateReceive`

**说明**：根据送货单创建收料单，自动编号（格式 `SLyyyyMMdd001`）。±5% 容差校验，全量收料后自动更新送货单和订单状态，同时更新库存。

**请求体**：
```json
{
  "noteCode": "DSH20260623001",
  "receiveUserID": "guid",
  "receiveUserName": "张三",
  "memo?": "收料备注",
  "details?": [
    {
      "materialCode": "WL001",
      "receivedQty": 50
    }
  ]
}
```

**成功响应（200）**：
```json
{
  "code": 200,
  "message": "收料单创建成功",
  "data": {
    "receiveID": "guid",
    "receiveCode": "SL20260623001",
    "noteID": "guid",
    "supplierID": "guid",
    "supplierName": "XX供应商",
    "receiveUserID": "guid",
    "receiveUserName": "张三",
    "receiveDate": "2026-06-23T10:00:00",
    "memo": null,
    "detailCount": 3,
    "isFullyReceived": false
  }
}
```

**失败**：
- `400` 参数为空 / 重复收料 / 数量超限
- `404` 送货单不存在 / 收料人不存在

---

### POST `Receive/list`

**说明**：分页查询收料单列表（含明细）

**请求体**：
```json
{
  "receiveCode?": "SL20260623",
  "noteCode?": "DSH20260623",
  "supplierId?": "guid",
  "page": 1,
  "pageSize": 10
}
```

**成功响应（200）**：
```json
{
  "code": 200,
  "data": {
    "items": [
      {
        "receiveID": "guid",
        "receiveCode": "SL20260623001",
        "noteID": "guid",
        "noteCode": "DSH20260623001",
        "supplierID": "guid",
        "supplierName": "XX供应商",
        "receiveUserID": "guid",
        "receiveUserName": "张三",
        "receiveDate": "2026-06-23T10:00:00",
        "memo": null,
        "details": [
          {
            "receiveDetailID": "guid",
            "materialCode": "WL001",
            "materialName": "电阻",
            "planQty": 100,
            "receivedQty": 50,
            "diffQty": 50,
            "unit": "个",
            "unitPrice": 5.50,
            "amount": 275
          }
        ]
      }
    ],
    "total": 10,
    "page": 1,
    "pageSize": 10
  }
}
```

---

## 附录 A：路由前缀汇总

| Controller | 路由前缀 | 响应格式 |
|-----------|---------|---------|
| LoginController | `/login` + `api/Login/[action]` | ApiResult 包装 |
| UserController | `[controller]/[action]` → `User/xxx` | ApiResult 包装 |
| SupplierController | `[controller]/[action]` → `Supplier/xxx` | ApiResult 包装 |
| MaterialsController | `[controller]/[action]` → `Materials/xxx` | ApiResult 包装 |
| OrdersController | `[controller]/[action]` → `Orders/xxx` | ApiResult 包装 |
| DeliveryController | `[controller]/[action]` → `Delivery/xxx` | 匿名对象（`code` + `message` + `data`） |
| ReceiveController | `[controller]/[action]` → `Receive/xxx` | 匿名对象（`code` + `message` + `data`） |

## 附录 B：订单状态枚举

| 值 | 说明 |
|----|------|
| 0 | 待确认 |
| 1 | 已确认 |
| 2 | 待发货 |
| 3 | 已发货 |
| 4 | 已收货 |

## 附录 C：响应结构对比

### ApiResult 格式（Login / User / Supplier / Materials / Orders）

```json
{ "success": true, "message": "操作成功", "data": { ... } }
```

### 匿名格式（Delivery / Receive）

```json
{ "code": 200, "message": "操作成功", "data": { ... } }
```
