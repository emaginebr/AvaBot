import ChatWidget from '../components/chat/ChatWidget'
import AvatarBubble from '../components/chat/AvatarBubble'

const AbipescaPage = () => {
  return (
    <div
      className="min-h-screen bg-cover bg-center bg-no-repeat"
      style={{ backgroundImage: `url(${import.meta.env.BASE_URL}abipesca.jpg)` }}
    >
      <ChatWidget
        slug="biia"
        greeting="Oi, eu sou a Biia. Posso ajudar?"
        color="#67a24a"
        agentAvatar={`${import.meta.env.BASE_URL}biia-foto.png`}
        renderBubble={(props) => (
          <AvatarBubble
            message={props.message}
            onClick={props.onClick}
            isOpen={props.isOpen}
            color={props.color}
            avatarSrc={`${import.meta.env.BASE_URL}biia.png`}
          />
        )}
      />
    </div>
  )
}

export default AbipescaPage
