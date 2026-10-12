import React, { useState, useEffect } from 'react'
import axios from 'axios'

interface Product {
  id: number
  name: string
  price: number
  stock: number
}

interface CartItem extends Product {
  quantity: number
}

export default function Home() {
  const [products, setProducts] = useState<Product[]>([])
  const [cart, setCart] = useState<CartItem[]>([])
  const [cartId, setCartId] = useState<string>('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const CATALOG_URL = process.env.NEXT_PUBLIC_CATALOG_URL || 'http://localhost:5001'
  const CART_URL = process.env.NEXT_PUBLIC_CART_URL || 'http://localhost:5002'
  const ORDER_URL = process.env.NEXT_PUBLIC_ORDER_URL || 'http://localhost:5003'

  useEffect(() => {
    initializeApp()
  }, [])

  const initializeApp = async () => {
    try {
      // Fetch products
      const productsRes = await axios.get(`${CATALOG_URL}/v1/catalog/products`)
      setProducts(productsRes.data.data)

      // Create new cart
      const cartRes = await axios.post(`${CART_URL}/v1/cart`)
      setCartId(cartRes.data.data.id)
      setLoading(false)
    } catch (err) {
      setError('Failed to initialize app')
      setLoading(false)
    }
  }

  const addToCart = async (product: Product) => {
    try {
      const response = await axios.post(
        `${CART_URL}/v1/cart/${cartId}/items?productId=${product.id}&quantity=1`
      )
      
      // Update local cart
      const existingItem = cart.find(item => item.id === product.id)
      if (existingItem) {
        existingItem.quantity += 1
      } else {
        setCart([...cart, { ...product, quantity: 1 }])
      }
    } catch (err) {
      setError('Failed to add item to cart')
    }
  }

  const removeFromCart = async (productId: number) => {
    try {
      await axios.delete(`${CART_URL}/v1/cart/${cartId}/items/${productId}`)
      setCart(cart.filter(item => item.id !== productId))
    } catch (err) {
      setError('Failed to remove item from cart')
    }
  }

  const checkout = async () => {
    try {
      const response = await axios.post(`${ORDER_URL}/v1/orders`, {
        cartId: cartId
      })
      alert(`Order created: ${response.data.data.id}`)
      setCart([])
      // Create new cart for next order
      const newCartRes = await axios.post(`${CART_URL}/v1/cart`)
      setCartId(newCartRes.data.data.id)
    } catch (err) {
      setError('Failed to create order')
    }
  }

  const totalPrice = cart.reduce((sum, item) => sum + (item.price * item.quantity), 0)

  if (loading) return <div>Loading...</div>
  if (error) return <div style={{ color: 'red' }}>{error}</div>

  return (
    <div style={{ fontFamily: 'Arial, sans-serif', padding: '20px', maxWidth: '1200px', margin: '0 auto' }}>
      <h1>🛒 MiniCart - Shopping Application</h1>
      <p>Learning microservices with OpenTelemetry observability</p>

      <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: '20px', marginTop: '20px' }}>
        {/* Products Section */}
        <div>
          <h2>Available Products</h2>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '10px' }}>
            {products.map(product => (
              <div key={product.id} style={{ border: '1px solid #ddd', padding: '10px', borderRadius: '5px' }}>
                <h3>{product.name}</h3>
                <p style={{ fontSize: '18px', fontWeight: 'bold', color: '#2563eb' }}>
                  ${product.price.toFixed(2)}
                </p>
                <p style={{ fontSize: '14px', color: '#666' }}>
                  Stock: {product.stock}
                </p>
                <button
                  onClick={() => addToCart(product)}
                  style={{
                    backgroundColor: '#2563eb',
                    color: 'white',
                    border: 'none',
                    padding: '8px 12px',
                    borderRadius: '4px',
                    cursor: 'pointer',
                    width: '100%',
                    marginTop: '10px'
                  }}
                >
                  Add to Cart
                </button>
              </div>
            ))}
          </div>
        </div>

        {/* Cart Section */}
        <div>
          <h2>Shopping Cart</h2>
          <div style={{ border: '1px solid #ddd', padding: '15px', borderRadius: '5px', backgroundColor: '#f9f9f9' }}>
            {cart.length === 0 ? (
              <p style={{ color: '#666' }}>Cart is empty</p>
            ) : (
              <>
                {cart.map(item => (
                  <div key={item.id} style={{ borderBottom: '1px solid #eee', paddingBottom: '10px', marginBottom: '10px' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <div>
                        <p style={{ margin: '0 0 5px 0', fontWeight: 'bold' }}>{item.name}</p>
                        <p style={{ margin: '0', fontSize: '14px', color: '#666' }}>
                          ${item.price.toFixed(2)} x {item.quantity}
                        </p>
                      </div>
                      <button
                        onClick={() => removeFromCart(item.id)}
                        style={{
                          backgroundColor: '#ef4444',
                          color: 'white',
                          border: 'none',
                          padding: '4px 8px',
                          borderRadius: '4px',
                          cursor: 'pointer',
                          fontSize: '12px'
                        }}
                      >
                        Remove
                      </button>
                    </div>
                  </div>
                ))}
                <div style={{ borderTop: '2px solid #ddd', paddingTop: '10px', marginTop: '10px' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '18px', fontWeight: 'bold', marginBottom: '15px' }}>
                    <span>Total:</span>
                    <span style={{ color: '#2563eb' }}>${totalPrice.toFixed(2)}</span>
                  </div>
                  <button
                    onClick={checkout}
                    style={{
                      backgroundColor: '#10b981',
                      color: 'white',
                      border: 'none',
                      padding: '10px',
                      borderRadius: '4px',
                      cursor: 'pointer',
                      width: '100%',
                      fontSize: '16px',
                      fontWeight: 'bold'
                    }}
                  >
                    Checkout
                  </button>
                </div>
              </>
            )}
          </div>
          <div style={{ marginTop: '15px', padding: '10px', backgroundColor: '#f0f9ff', borderRadius: '4px', fontSize: '12px' }}>
            <p style={{ margin: '0', color: '#666' }}>
              <strong>Cart ID:</strong> {cartId.substring(0, 8)}...
            </p>
          </div>
        </div>
      </div>

      {/* Info Section */}
      <div style={{ marginTop: '40px', padding: '20px', backgroundColor: '#f3f4f6', borderRadius: '5px' }}>
        <h3>📊 Microservices Architecture</h3>
        <ul style={{ fontSize: '14px' }}>
          <li>🏪 <strong>CatalogService</strong> (5001): Product catalog - no dependencies</li>
          <li>🛒 <strong>CartService</strong> (5002): Shopping cart - calls CatalogService</li>
          <li>📦 <strong>OrderService</strong> (5003): Order processing - calls CartService</li>
          <li>🔍 <strong>Observability Stack</strong>: Traces, metrics, and logs via OpenTelemetry</li>
        </ul>
      </div>
    </div>
  )
}
